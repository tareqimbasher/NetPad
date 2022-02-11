using System;
using System.Diagnostics;
using System.Threading.Tasks;
using NetPad.OmniSharpWrapper.Utilities;

namespace NetPad.OmniSharpWrapper.Stdio
{
    internal class OmniSharpServerStdioProcessAccessor : IOmniSharpServerProcessAccessor<ProcessIOHandler>, IDisposable
    {
        private readonly OmniSharpStdioServerConfiguration _configuration;
        private ProcessHandler? _processHandler;
        private ProcessIOHandler? _processIoHandler;

        public OmniSharpServerStdioProcessAccessor(OmniSharpStdioServerConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<ProcessIOHandler> GetEntryPointAsync()
        {
            if (_configuration.ShouldCreateNewProcess)
            {
                if (_processHandler != null)
                    throw new Exception("OmniSharp server is already initialized.");
                
                _processHandler = new ProcessHandler(_configuration.ExecutablePath!, _configuration.ExecutableArgs!);
                
                if (!await _processHandler.RunAsync(false) || _processHandler.Process == null || _processHandler.ProcessIO == null)
                {
                    throw new Exception($"Could not run process at: {_configuration.ExecutablePath}. " +
                                        $"Args: {_configuration.ExecutableArgs}");
                }

                return _processHandler.ProcessIO;
            }
            else
            {
                if (_configuration.Process == null)
                    throw new Exception("Could not get process. Process is null.");
                
                var process = _configuration.Process;
                if (!process.IsProcessRunning())
                    throw new Exception("OmniSharp process is not running.");

                _processIoHandler = new ProcessIOHandler(process);
                
                return _processIoHandler;
            }
        }

        public async Task StopProcessAsync()
        {
            if (_configuration.ShouldCreateNewProcess)
            {
                _processHandler?.StopProcess();
                _processHandler?.Dispose();
            }
            else
            {
                // Do nothing. Process is controlled externally.
            }
        }

        public void Dispose()
        {
            AsyncHelpers.RunSync(StopProcessAsync);
        }
    }
}