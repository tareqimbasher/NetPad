using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OmniSharp.Mef;
using OmniSharp.Stdio.IO;
using OmniSharp.Utilities;

namespace OmniSharp.Stdio
{
    public class OmniSharpStdioServer : OmniSharpServer<OmniSharpStdioServerConfiguration>
    {
        private readonly IOmniSharpServerProcessAccessor<ProcessIOHandler> _omniSharpServerProcessAccessor;
        private readonly RequestResponseQueue _requestResponseQueue;
        private ProcessIOHandler? _processIo;
        private bool _isStopped = true;

        public OmniSharpStdioServer(
            OmniSharpStdioServerConfiguration configuration,
            IOmniSharpServerProcessAccessor<ProcessIOHandler> omniSharpServerProcessAccessor,
            ILoggerFactory loggerFactory) :
            base(configuration, loggerFactory)
        {
            _omniSharpServerProcessAccessor = omniSharpServerProcessAccessor;
            _requestResponseQueue = new RequestResponseQueue();
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

        public override Task Send(object request) => Send<NoResponse>(request);

        public override async Task<TResponse?> Send<TResponse>(object request) where TResponse : class
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (_processIo == null)
                throw new InvalidOperationException("Server is not started.");

            if (_isStopped)
                throw new InvalidOperationException("Server is stopped.");

            if (request.GetType().GetCustomAttribute(typeof(OmniSharpEndpointAttribute), true) is not OmniSharpEndpointAttribute endpointAttribute)
                throw new ArgumentException($"Could not get endpoint name from OmniSharp request type: {request.GetType().FullName}. " +
                                            $"Make sure the request is decorated with a {nameof(OmniSharpEndpointAttribute)} or inherits from a " +
                                            $"type that is decorated with such an attribute.");

            var requestPacket = new RequestPacket(NextSequence(), endpointAttribute.EndpointName, request);

            var responsePromise = _requestResponseQueue.Enqueue(requestPacket);

            await _processIo.StandardInput.WriteLineAsync(JsonConvert.SerializeObject(requestPacket)).ConfigureAwait(false);

            var responseJToken = await responsePromise.ConfigureAwait(false);

            bool success = responseJToken.Success();

            if (typeof(TResponse) != typeof(NoResponse))
            {
                return responseJToken.Body<TResponse>();
            }

            return null;
        }


        private Task HandleOmniSharpErrorOutput(string error)
        {
            if (string.IsNullOrEmpty(error)) return Task.CompletedTask;

            error = StringUtils.RemoveBOMString(error);
            Logger.LogError($"OmniSharpServer Error: {error}");

            return Task.CompletedTask;
        }


        private async Task HandleOmniSharpDataOutput(string output)
        {
            if (string.IsNullOrEmpty(output)) return;

            output = StringUtils.RemoveBOMString(output);

            if (output[0] != '{')
            {
                return;
            }

            var outputPacket = JObject.Parse(output);
            var packetType = (string?)outputPacket["Type"];

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
                Logger.LogError($"Error while handling omnisharp output. {ex}");
            }
        }

        private Task HandleResponsePacketReceived(JObject response)
        {
            Logger.LogDebug($"OmniSharpServer Response: {response}");

            var responseJObject = new ResponseJObject(response);

            _requestResponseQueue.HandleResponse(responseJObject);

            return Task.CompletedTask;
        }

        private Task HandleEventPacketReceived(JObject eventPacket)
        {
            var @event = (string?)eventPacket["Event"];

            if (@event == "log")
                Logger.LogDebug($"OmniSharpServer Event Log: {eventPacket}");
            else if (@event == "Error")
                Logger.LogDebug($"OmniSharpServer Error Log: {eventPacket}");

            return Task.CompletedTask;
        }
    }
}
