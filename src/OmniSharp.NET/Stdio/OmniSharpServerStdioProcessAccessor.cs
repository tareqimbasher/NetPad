using System;
using System.Threading.Tasks;
using OmniSharp.Utilities;

namespace OmniSharp.Stdio
{
    internal class OmniSharpServerStdioProcessAccessor : IOmniSharpServerProcessAccessor<ProcessIOHandler>, IDisposable
    {
        private readonly OmniSharpStdioServerConfiguration _configuration;
        private ProcessHandler? _processHandler;
        private ProcessIOHandler? _processIoHandler;

        public OmniSharpServerStdioProcessAccessor(OmniSharpStdioServerConfiguration configuration)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        public async Task<ProcessIOHandler> GetEntryPointAsync()
        {
            if (_processIoHandler != null)
                return _processIoHandler;

            if (_configuration.ExternallyManagedProcess)
            {
                var process = _configuration.ProcessGetter!();

                if (!process.IsProcessRunning())
                    throw new Exception("Externally managed OmniSharpServer process is not running.");

                _processIoHandler = new ProcessIOHandler(process);
            }
            else
            {
                if (_processHandler != null)
                    throw new Exception("OmniSharp server is already initialized.");

                var exePath = _configuration.ExecutablePath!;
                var exeArgs = _configuration.ExecutableArgs!;
                
                _processHandler = new ProcessHandler(exePath, exeArgs);

                if (!await _processHandler.StartAsync(false) || _processHandler.Process == null || _processHandler.ProcessIO == null)
                    throw new Exception($"Could not start process at: {exePath}. Args: {exeArgs}");

                _processIoHandler = _processHandler.ProcessIO;
            }

            return _processIoHandler;
        }

        public Task StopProcessAsync()
        {
            if (_configuration.ExternallyManagedProcess)
            {
                // Do nothing. Process is controlled externally.
            }
            else
            {
                _processIoHandler?.Dispose();
                _processHandler?.StopProcess();
                _processHandler?.Dispose();
            }

            _processHandler = null;
            _processIoHandler = null;

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            AsyncHelpers.RunSync(StopProcessAsync);
        }
    }
}