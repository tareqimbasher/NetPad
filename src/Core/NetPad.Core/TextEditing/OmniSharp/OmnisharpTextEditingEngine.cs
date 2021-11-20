using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetPad.Scripts;
using NetPad.Utilities;

namespace NetPad.TextEditing.OmniSharp
{
    public class OmniSharpTextEditingEngine : ITextEditingEngine, IDisposable
    {
        private readonly ILogger<OmniSharpTextEditingEngine> _logger;
        private ProcessHandler? _omnisharpProcess;

        public OmniSharpTextEditingEngine(ILogger<OmniSharpTextEditingEngine> logger)
        {
            _logger = logger;
        }
        
        public async Task LoadAsync(Script script)
        {
            if (_omnisharpProcess != null)
                throw new Exception("Omnisharp process is already initialized.");

            var omnisharpCmd = "/home/tips/X/tmp/Omnisharp/omnisharp-linux-x64/run";

            var args = new List<string>
            {
                // "-z", // Zero-based indicies
                "-s", "/home/tips/Source/tmp/test-project",
                "--hostPID", Process.GetCurrentProcess().Id.ToString(),
                "DotNet:enablePackageRestore=false",
                "--encoding", "utf-8",
                //"--loglevel", "LogLevel.Debug", // replace with actual enum
                "-v", // Sets --loglevel to debug
            };

            args.Add("RoslynExtensionsOptions:EnableImportCompletion=true");
            args.Add("RoslynExtensionsOptions:EnableAnalyzersSupport=true");
            args.Add("RoslynExtensionsOptions:EnableAsyncCompletion=true");

            _omnisharpProcess = new ProcessHandler(omnisharpCmd, string.Join(" ", args));
            
            _omnisharpProcess.OnOutputReceivedHandlers.Add(HandleOmniSharpOutput);
            _omnisharpProcess.OnErrorReceivedHandlers.Add(HandleOmniSharpError);

            if (!await _omnisharpProcess.RunAsync())
                throw new Exception($"Failed to run omnisharp.");
        }

        public async Task Autocomplete()
        {
            await _omnisharpProcess.StandardInput.WriteLineAsync(
                "{" +
                "\"Command\": \"/completion\", " +
                "\"Seq\": 4500, " +
                "\"Line\": \"9\", " +
                "\"Column\": \"21\", " +
                "\"FileName\": \"test-project/Program.cs\"," +
                "\"CompletionTrigger\": 1" +
                "}");
        }

        public void Dispose()
        {
            _omnisharpProcess?.Dispose();
        }

        private async Task HandleOmniSharpOutput(string output)
        {
            output = StringUtils.RemoveBOMString(output);

            if (output[0] != '{')
            {
                _logger.LogInformation($"OMNISHARP OUTPUT RAW: {output}");
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
                    _logger.LogError($"Unknown packet type: ${packetType}");
                    break;
            }
                
            _logger.LogInformation($"OMNISHARP OUTPUT: {output}");
        }

        private async Task HandleOmniSharpError(string error)
        {
            error = StringUtils.RemoveBOMString(error);
            _logger.LogError($"OMNISHARP ERROR: {error}");
        }

        private async Task HandleEventPacketReceived(string json)
        {
            try
            {
                var packet = JsonSerializer.Deserialize<OmniSharpEventPacket>(json);
                if (packet.Event == "log")
                {
                    _logger.LogDebug($"OMNISHARP LOG: {json}");
                }
                else if (packet.Event == "Error")
                {
                    _logger.LogDebug($"OMNISHARP LOG ERROR: {json}");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }
        }
        
        private async Task HandleResponsePacketReceived(string json)
        {
            try
            {
                var packet = JsonSerializer.Deserialize<OmniSharpResponsePacket>(json);
                _logger.LogInformation($"OMNISHARP RESPONSE: {json}");
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
            }
            
        }
    }
}