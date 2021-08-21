using System;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetPad.OmniSharpWrapper.Utilities;
using OmniSharp.Mef;

namespace NetPad.OmniSharpWrapper.Stdio
{
    public class OmniSharpStdioServer : OmniSharpServer<OmniSharpStdioServerConfiguration>
    {
        private readonly IOmniSharpServerProcessAccessor<ProcessIOHandler> _omniSharpServerProcessAccessor;
        private ProcessIOHandler? _processIo;

        public OmniSharpStdioServer(
            OmniSharpStdioServerConfiguration configuration,
            IOmniSharpServerProcessAccessor<ProcessIOHandler> omniSharpServerProcessAccessor,
            ILogger<OmniSharpStdioServer> logger) :
            base(configuration, logger)
        {
            _omniSharpServerProcessAccessor = omniSharpServerProcessAccessor;
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

            var endpointAttribute = request.GetType().GetCustomAttribute(typeof(OmniSharpEndpointAttribute)) as OmniSharpEndpointAttribute;
            if (endpointAttribute == null)
                throw new Exception($"Could not get endpoint name of OmniSharp request type: {request.GetType().FullName}");

            var requestPacket = new
            {
                Command = endpointAttribute.EndpointName,
                Seq = ++Sequence,
                Arguments = request
            };

            await _processIo.StandardInput.WriteLineAsync(JsonSerializer.Serialize(requestPacket));

            return null;
        }
        
        
        
        private async Task HandleOmniSharpOutput(string output)
        {
            output = StringUtils.RemoveBOMString(output);

            if (output[0] != '{')
            {
                Logger.LogInformation($"OMNISHARP OUTPUT RAW: {output}");
                return;
            }
                
            var packetType = JsonSerializer.Deserialize<OmniSharpPacket>(output)?.Type;
            if (packetType == null)
            {
                // Bogus packet
                return;
            }

            switch (packetType)
            {
                case "response":
                    await HandleResponsePacketReceived(output);
                    break;
                case "event":
                    await HandleEventPacketReceived(output);
                    break;
                default:
                    Logger.LogError($"Unknown packet type: ${packetType}");
                    break;
            }
                
            Logger.LogInformation($"OMNISHARP OUTPUT: {output}");
        }
        
        private async Task HandleOmniSharpError(string error)
        {
            error = StringUtils.RemoveBOMString(error);
            Logger.LogError($"OMNISHARP ERROR: {error}");
        }

        private async Task HandleEventPacketReceived(string json)
        {
            try
            {
                var packet = JsonSerializer.Deserialize<OmniSharpEventPacket>(json);
                if (packet.Event == "log")
                {
                    Logger.LogDebug($"OMNISHARP LOG: {json}");
                }
                else if (packet.Event == "Error")
                {
                    Logger.LogDebug($"OMNISHARP LOG ERROR: {json}");
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }
        }
        
        private async Task HandleResponsePacketReceived(string json)
        {
            try
            {
                var packet = JsonSerializer.Deserialize<OmniSharpResponsePacket>(json);
                Logger.LogInformation($"OMNISHARP RESPONSE: {json}");
            }
            catch (Exception e)
            {
                Logger.LogError(e.ToString());
            }
        }
    }
}