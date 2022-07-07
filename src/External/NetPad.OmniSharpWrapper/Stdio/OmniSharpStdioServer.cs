using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetPad.OmniSharpWrapper.Stdio.Models;
using NetPad.OmniSharpWrapper.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OmniSharp.Mef;

namespace NetPad.OmniSharpWrapper.Stdio
{
    public class OmniSharpStdioServer : OmniSharpServer<OmniSharpStdioServerConfiguration>
    {
        private readonly IOmniSharpServerProcessAccessor<ProcessIOHandler> _omniSharpServerProcessAccessor;
        private ProcessIOHandler? _processIo;
        private readonly RequestResponseQueue _requestResponseQueue;

        public OmniSharpStdioServer(
            OmniSharpStdioServerConfiguration configuration,
            IOmniSharpServerProcessAccessor<ProcessIOHandler> omniSharpServerProcessAccessor,
            ILoggerFactory loggerFactory) :
            base(configuration, loggerFactory)
        {
            _omniSharpServerProcessAccessor = omniSharpServerProcessAccessor;
            _requestResponseQueue = new RequestResponseQueue(loggerFactory.CreateLogger<RequestResponseQueue>());
        }

        public override async Task StartAsync()
        {
            _processIo = await _omniSharpServerProcessAccessor.GetEntryPointAsync();
            _processIo.OnOutputReceivedHandlers.Add(HandleOmniSharpOutput);
            _processIo.OnErrorReceivedHandlers.Add(HandleOmniSharpError);
        }

        public override async Task StopAsync()
        {
            await _omniSharpServerProcessAccessor.StopProcessAsync();
        }

        public override async Task<TResponse> Send<TResponse>(object request)
        {
            if (_processIo == null)
                throw new Exception("OmniSharp Server is not started.");

            if (request.GetType().GetCustomAttribute(typeof(OmniSharpEndpointAttribute), true) is not OmniSharpEndpointAttribute endpointAttribute)
                throw new Exception($"Could not get endpoint name of OmniSharp request type: {request.GetType().FullName}");

            var requestPacket = new RequestPacket(++Sequence, endpointAttribute.EndpointName, request);

            var responsePromise = _requestResponseQueue.Enqueue(requestPacket);

            await _processIo.StandardInput.WriteLineAsync(JsonConvert.SerializeObject(requestPacket)).ConfigureAwait(false);

            var responseJToken = await responsePromise.ConfigureAwait(false);

            var response = responseJToken.ToObject<TResponse>();

            if (response == null)
                throw new Exception("Bad response");

            return response;
        }

        private async Task HandleOmniSharpOutput(string output)
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
                    Logger.LogError("Unknown packet type: {PacketType}", packetType);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error while handling omnisharp output");
            }
        }

        private Task HandleOmniSharpError(string error)
        {
            if (string.IsNullOrEmpty(error)) return Task.CompletedTask;

            error = StringUtils.RemoveBOMString(error);
            Logger.LogError("OmniSharp server error: {Error}", error);

            return Task.CompletedTask;
        }

        private Task HandleEventPacketReceived(JObject eventPacket)
        {
            var @event = (string?)eventPacket["Event"];

            if (@event == "log")
                Logger.LogDebug("OmniSharp server log: {Log}", eventPacket.ToString());
            else if (@event == "Error")
                Logger.LogDebug("OmniSharp server error: {Error}", eventPacket.ToString());

            return Task.CompletedTask;
        }

        private Task HandleResponsePacketReceived(JObject response)
        {
            Logger.LogDebug("OmniSharp server response received: {Response}", response.ToString());

            var requestSeq = (int)(response["Request_seq"] ?? throw new Exception("Response did not have a value for 'Request_seq'"));
            var responseJToken = response["Body"] ?? throw new Exception("Response did not have a value for 'Body'");
            var command = (string?)response["Command"];

            _requestResponseQueue.HandleResponse(requestSeq, command, responseJToken);

            return Task.CompletedTask;
        }
    }
}
