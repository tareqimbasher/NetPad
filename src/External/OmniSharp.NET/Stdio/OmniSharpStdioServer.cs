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
    internal class OmniSharpStdioServer : OmniSharpServer<OmniSharpStdioServerConfiguration>, IOmniSharpStdioServer
    {
        private readonly IOmniSharpServerProcessAccessor<ProcessIO> _omniSharpServerProcessAccessor;
        private readonly RequestResponseQueue _requestResponseQueue;
        private readonly ConcurrentDictionary<string, List<Func<JsonNode, Task>>> _eventHandlers;
        private readonly object _stdioStandardInputLock;
        private ProcessIO? _processIo;
        private bool _isStopped = true;

        private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            IncludeFields = true // To serialize Tuples
        };

        public OmniSharpStdioServer(
            OmniSharpStdioServerConfiguration configuration,
            IOmniSharpServerProcessAccessor<ProcessIO> omniSharpServerProcessAccessor,
            ILoggerFactory loggerFactory) :
            base(configuration, loggerFactory)
        {
            _omniSharpServerProcessAccessor = omniSharpServerProcessAccessor;
            _requestResponseQueue = new RequestResponseQueue();
            _eventHandlers = new ConcurrentDictionary<string, List<Func<JsonNode, Task>>>();
            _stdioStandardInputLock = new object();
        }

        public bool IsProcessRunning() => _processIo is { Process: { HasExited: false } };

        public override async Task StartAsync()
        {
            _processIo = await _omniSharpServerProcessAccessor.GetEntryPointAsync().ConfigureAwait(false);

            _processIo.OnOutputReceivedHandlers.Add(HandleOmniSharpDataOutput);
            _processIo.OnErrorReceivedHandlers.Add(HandleOmniSharpErrorOutput);

            _isStopped = false;
        }

        public override async Task StopAsync()
        {
            await _omniSharpServerProcessAccessor.StopProcessAsync().ConfigureAwait(false);
            _processIo?.Dispose();
            _isStopped = true;
        }

        public void AddOnProcessOutputReceivedHandler(Func<string, Task> handler)
        {
            _processIo?.OnOutputReceivedHandlers.Add(handler);
        }

        public void RemoveOnProcessOutputReceivedHandler(Func<string, Task> handler)
        {
            _processIo?.OnOutputReceivedHandlers.Remove(handler);
        }

        public void AddOnProcessErrorReceivedHandler(Func<string, Task> handler)
        {
            _processIo?.OnErrorReceivedHandlers.Add(handler);
        }

        public void RemoveOnProcessErrorReceivedHandler(Func<string, Task> handler)
        {
            _processIo?.OnErrorReceivedHandlers.Remove(handler);
        }

        public override Task SendAsync(object request, CancellationToken? cancellationToken = default) =>
            SendAsync<NoResponse>(request, cancellationToken);

        public override Task<TResponse?> SendAsync<TResponse>(object request,
            CancellationToken? cancellationToken = default) where TResponse : class
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var endpointAttribute = GetOmniSharpEndpointAttribute(request);

            return SendAsync<TResponse>(endpointAttribute.EndpointName, request, cancellationToken);
        }

        public override Task SendAsync<TRequest>(IEnumerable<TRequest> requests,
            CancellationToken? cancellationToken = default) =>
            SendAsync<TRequest, NoResponse>(requests, cancellationToken);

        public override Task<TResponse?> SendAsync<TRequest, TResponse>(IEnumerable<TRequest> requests,
            CancellationToken? cancellationToken = default)
            where TResponse : class
        {
            if (!requests.Any())
                throw new ArgumentException($"{nameof(requests)} is empty.");

            var endpointAttribute = GetOmniSharpEndpointAttribute(requests);

            return SendAsync<TResponse>(endpointAttribute.EndpointName, requests, cancellationToken);
        }

        public override async Task<TResponse?> SendAsync<TResponse>(string endpointName, object request, CancellationToken? cancellationToken = default)
            where TResponse : class
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (_processIo == null)
                throw new InvalidOperationException("Server is not started.");

            if (_isStopped)
                throw new InvalidOperationException("Server is stopped.");

            if (cancellationToken?.IsCancellationRequested == true)
            {
                return null;
            }

            var requestPacket = new RequestPacket(NextSequence(), endpointName, request);

            var responsePromise =
                _requestResponseQueue.Enqueue(requestPacket, cancellationToken ?? CancellationToken.None);

            try
            {
                string requestJson = JsonSerializer.Serialize(requestPacket, _jsonSerializerOptions);

                lock (_stdioStandardInputLock)
                {
                    _processIo.StandardInput.WriteLine(requestJson);
                }

                var responseJToken = await responsePromise.ConfigureAwait(false);

                if (responseJToken == null)
                {
                    return null;
                }

                bool success = responseJToken.Success();

                if (success && typeof(TResponse) != typeof(NoResponse))
                {
                    return responseJToken.Body<TResponse>(_jsonSerializerOptions);
                }

                return null;
            }
            catch (TaskCanceledException)
            {
                // Request was cancelled
                return null;
            }
            catch
            {
                _requestResponseQueue.WaitingForResponseFailed(requestPacket);
                throw;
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
            _eventHandlers.Clear();
            base.Dispose();
        }

        private OmniSharpEndpointAttribute GetOmniSharpEndpointAttribute(object obj)
        {
            var requestType = obj.GetType();
            if (requestType.IsArray)
            {
                requestType = ((IEnumerable<object>)obj).ElementAt(0).GetType();
            }

            if (requestType.GetCustomAttribute(typeof(OmniSharpEndpointAttribute), true) is not
                OmniSharpEndpointAttribute endpointAttribute)
            {
                throw new ArgumentException(
                    $"Request of type '{obj.GetType().FullName}' does not have a {nameof(OmniSharpEndpointAttribute)} " +
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
                    await HandleResponsePacketReceived(outputPacket).ConfigureAwait(false);
                else if (packetType == "event")
                    await HandleEventPacketReceived(outputPacket).ConfigureAwait(false);
                else
                    Logger.LogError("Unknown packet type: {PacketType}", packetType);
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

            if (@event == "log")
            {
                LogEvent(eventPacket);
            }

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

        private void LogEvent(JsonNode eventPacket)
        {
            try
            {
                var omniSharpEvent = eventPacket["Body"].Deserialize<OmniSharpEvent>();

                if (omniSharpEvent == null) return;

                var logger = _loggerFactory.CreateLogger(omniSharpEvent.Name);

                switch (omniSharpEvent.LogLevel.ToUpperInvariant())
                {
                    case "TRACE":
                        logger.LogTrace(omniSharpEvent.Message);
                        break;
                    case "DEBUG":
                        logger.LogDebug(omniSharpEvent.Message);
                        break;
                    case "INFORMATION":
                        logger.LogInformation(omniSharpEvent.Message);
                        break;
                    case "WARNING":
                        logger.LogWarning(omniSharpEvent.Message);
                        break;
                    case "ERROR":
                        logger.LogError(omniSharpEvent.Message);
                        break;
                    case "CRITICAL":
                        logger.LogCritical(omniSharpEvent.Message);
                        break;
                }

                ;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Could not log OmniSharp event: {Event}", eventPacket.ToString());
            }
        }
    }
}
