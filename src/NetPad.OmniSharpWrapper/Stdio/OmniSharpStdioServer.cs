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
            _requestResponseQueue = new RequestResponseQueue();
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

        public override async Task<TResponse> Send<TRequest, TResponse>(TRequest request)
        {
            if (_processIo == null)
                throw new Exception("OmniSharp Server is not started.");

            var endpointAttribute = 
                (
                    request.GetType().GetCustomAttribute(typeof(OmniSharpEndpointAttribute))
                        ?? typeof(TRequest).GetCustomAttribute(typeof(OmniSharpEndpointAttribute))
                ) as OmniSharpEndpointAttribute;
            
            if (endpointAttribute == null)
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
                    Logger.LogError($"Unknown packet type: ${packetType}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error while handling omnisharp output. {ex}");
            }
        }
        
        private Task HandleOmniSharpError(string error)
        {
            if (string.IsNullOrEmpty(error)) return Task.CompletedTask;
            
            error = StringUtils.RemoveBOMString(error);
            Logger.LogError($"OMNISHARP ERROR: {error}");

            return Task.CompletedTask;
        }

        private Task HandleEventPacketReceived(JObject eventPacket)
        {
            var @event = (string?)eventPacket["Event"];
                
            if (@event == "log")
                Logger.LogDebug($"OMNISHARP LOG: {eventPacket}");
            else if (@event == "Error")
                Logger.LogDebug($"OMNISHARP LOG ERROR: {eventPacket}");

            return Task.CompletedTask;
        }
        
        private Task HandleResponsePacketReceived(JObject response)
        {
            Logger.LogDebug($"OMNISHARP RESPONSE: {response}");
                
            var requestSeq = (int)(response["Request_seq"] ?? throw new Exception("Response did not have a value for 'Request_seq'"));
            var responseJToken = response["Body"] ?? throw new Exception("Response did not have a value for 'Body'");
                
            _requestResponseQueue.HandleResponse(requestSeq, responseJToken);

            return Task.CompletedTask;
        }
    }
}