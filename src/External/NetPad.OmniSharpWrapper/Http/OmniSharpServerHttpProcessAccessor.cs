using System;
using System.Threading.Tasks;
using NetPad.OmniSharpWrapper.Utilities;

namespace NetPad.OmniSharpWrapper.Http
{
    public class OmniSharpServerHttpProcessAccessor : IOmniSharpServerProcessAccessor<string>, IDisposable
    {
        private readonly OmniSharpHttpServerConfiguration _configuration;
        private ProcessHandler? _processHandler;

        public OmniSharpServerHttpProcessAccessor(OmniSharpHttpServerConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<string> GetEntryPointAsync()
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

                return ""; // TODO return uri
            }
            else
            {
                if (_configuration.Uri == null)
                    throw new Exception("Uri is null.");

                return _configuration.Uri;
            }
        }

        public Task StopProcessAsync()
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

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            AsyncHelpers.RunSync(StopProcessAsync);
        }
    }
}
