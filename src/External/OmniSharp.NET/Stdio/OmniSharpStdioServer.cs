using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniSharp.Events;
using OmniSharp.Mef;
using OmniSharp.Stdio.IO;
using OmniSharp.Utilities;

namespace OmniSharp.Stdio
{
    public class OmniSharpStdioServer : OmniSharpServer<OmniSharpStdioServerConfiguration>, IOmniSharpStdioServer
    {
        private readonly IOmniSharpServerProcessAccessor<ProcessIOHandler> _omniSharpServerProcessAccessor;
        private readonly RequestResponseQueue _requestResponseQueue;
        private ProcessIOHandler? _processIo;
        private bool _isStopped = true;
        private readonly ConcurrentDictionary<string, List<Func<JsonNode, Task>>> _eventHandlers;
        private readonly SemaphoreSlim _semaphoreSlim;

        private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            IncludeFields = true // To serialize Tuples
        };

        public OmniSharpStdioServer(
            OmniSharpStdioServerConfiguration configuration,
            IOmniSharpServerProcessAccessor<ProcessIOHandler> omniSharpServerProcessAccessor,
            ILoggerFactory loggerFactory) :
            base(configuration, loggerFactory)
        {
            _omniSharpServerProcessAccessor = omniSharpServerProcessAccessor;
            _requestResponseQueue = new RequestResponseQueue();
            _semaphoreSlim = new SemaphoreSlim(1);
            _eventHandlers = new ConcurrentDictionary<string, List<Func<JsonNode, Task>>>();
        }

        public override async Task StartAsync()
        {
            _processIo = await _omniSharpServerProcessAccessor.GetEntryPointAsync();
            _processIo.OnOutputReceivedHandlers.Add(HandleOmniSharpDataOutput);
            _processIo.OnErrorReceivedHandlers.Add(HandleOmniSharpErrorOutput);
            _isStopped = false;
        }

        public override async Task StopAsync()
        {
            await _omniSharpServerProcessAccessor.StopProcessAsync();
            _isStopped = true;
        }

        public override Task SendAsync(object request) => SendAsync<NoResponse>(request);

        public override Task<TResponse?> SendAsync<TResponse>(object request) where TResponse : class
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var endpointAttribute = GetOmniSharpEndpointAttribute(request);

            return SendAsync<TResponse>(endpointAttribute.EndpointName, request);
        }

        public override Task SendAsync<TRequest>(IEnumerable<TRequest> requests) => SendAsync<TRequest, NoResponse>(requests);

        public override Task<TResponse?> SendAsync<TRequest, TResponse>(IEnumerable<TRequest> requests) where TResponse : class
        {
            if (!requests.Any())
                throw new ArgumentException($"{nameof(requests)} is empty.");

            var endpointAttribute = GetOmniSharpEndpointAttribute(requests);

            return SendAsync<TResponse>(endpointAttribute.EndpointName, requests);
        }

        public override async Task<TResponse?> SendAsync<TResponse>(string endpointName, object request) where TResponse : class
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (_processIo == null)
                throw new InvalidOperationException("Server is not started.");

            if (_isStopped)
                throw new InvalidOperationException("Server is stopped.");

            await _semaphoreSlim.WaitAsync();

            try
            {
                var requestPacket = new RequestPacket(NextSequence(), endpointName, request);

                var responsePromise = _requestResponseQueue.Enqueue(requestPacket);

                string requestJson = JsonSerializer.Serialize(requestPacket, _jsonSerializerOptions);

                await _processIo.StandardInput.WriteLineAsync(requestJson).ConfigureAwait(false);

                var responseJToken = await responsePromise.ConfigureAwait(false);

                bool success = responseJToken.Success();

                if (success && typeof(TResponse) != typeof(NoResponse))
                {
                    return responseJToken.Body<TResponse>(_jsonSerializerOptions);
                }

                return null;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public SubscriptionToken SubscribeToEvent(string eventType, Func<JsonNode, Task> handler)
        {
            var handlers = _eventHandlers.GetOrAdd(eventType.ToLowerInvariant(), new List<Func<JsonNode, Task>>());

            lock (handlers)
            {
                handlers.Add(handler);
            }

            return new SubscriptionToken(() =>
            {
                lock (handlers)
                {
                    handlers.Remove(handler);
                }
            });
        }

        public override void Dispose()
        {
            _semaphoreSlim.Dispose();
            base.Dispose();
        }

        private OmniSharpEndpointAttribute GetOmniSharpEndpointAttribute(object obj)
        {
            var requestType = obj.GetType();
            if (requestType.IsArray)
            {
                requestType = ((IEnumerable<object>)obj).ElementAt(0).GetType();
            }

            if (requestType.GetCustomAttribute(typeof(OmniSharpEndpointAttribute), true) is not OmniSharpEndpointAttribute endpointAttribute)
            {
                throw new ArgumentException($"Request of type '{obj.GetType().FullName}' does not have a {nameof(OmniSharpEndpointAttribute)} " +
                                            $"and is not an {nameof(IEnumerable)} that contains items that have the {nameof(OmniSharpEndpointAttribute)}.");
            }

            return endpointAttribute;
        }

        private Task HandleOmniSharpErrorOutput(string error)
        {
            if (string.IsNullOrEmpty(error)) return Task.CompletedTask;

            error = StringUtils.RemoveBOMString(error);
            Logger.LogError("OmniSharpServer Error Output: {Output}", error);

            return Task.CompletedTask;
        }


        private async Task HandleOmniSharpDataOutput(string output)
        {
            if (string.IsNullOrWhiteSpace(output)) return;

            output = StringUtils.RemoveBOMString(output);

            if (output[0] != '{')
            {
                return;
            }

            var outputPacket = JsonNode.Parse(output);
            if (outputPacket == null)
            {
                return;
            }

            var packetType = outputPacket["Type"]?.ToString();

            if (packetType == null)
            {
                // Bogus packet
                return;
            }

            try
            {
                if (packetType == "response")
                    await HandleResponsePacketReceived(outputPacket);
                else if (packetType == "event")
                    await HandleEventPacketReceived(outputPacket);
                else
                    Logger.LogError($"Unknown packet type: ${packetType}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while handling omnisharp output");
            }
        }

        private Task HandleResponsePacketReceived(JsonNode response)
        {
            Logger.LogDebug("OmniSharpServer Response Output: {Output}", response.ToString());

            var responseJObject = new ResponseJsonObject(response);

            _requestResponseQueue.HandleResponse(responseJObject);

            return Task.CompletedTask;
        }

        private Task HandleEventPacketReceived(JsonNode eventPacket)
        {
            var @event = ((string?)eventPacket["Event"])?.ToLowerInvariant();

            Logger.LogDebug("OmniSharpServer Log Output. Event type: {EventType}. Output: {Output}", @event, eventPacket.ToString());

            if (@event is not null && _eventHandlers.TryGetValue(@event, out var handlers))
            {
                foreach (var handler in handlers.ToArray())
                {
                    try
                    {
                        _ = handler(eventPacket);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError(ex, "Error executing handler for event type: {EventType}", @event);
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
