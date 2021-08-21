using System;
using System.Threading.Tasks;
using NetPad.OmniSharpWrapper.Utilities;
using Newtonsoft.Json;

namespace NetPad.OmniSharpWrapper.Stdio
{
    public class OmniSharpStdioServer : OmniSharpServer<OmniSharpStdioServerConfiguration>
    {
        private readonly IOmniSharpServerProcessAccessor<ProcessIOHandler> _omniSharpServerProcessAccessor;
        private ProcessIOHandler? _processIo;

        public OmniSharpStdioServer(OmniSharpStdioServerConfiguration configuration, IOmniSharpServerProcessAccessor<ProcessIOHandler> omniSharpServerProcessAccessor) :
            base(configuration)
        {
            _omniSharpServerProcessAccessor = omniSharpServerProcessAccessor;
        }

        public override async Task StartAsync()
        {
            _processIo = await _omniSharpServerProcessAccessor.GetEntryPointAsync();
        }

        public override async Task StopAsync()
        {
            await _omniSharpServerProcessAccessor.StopProcessAsync();
        }

        public override async Task<TResponse> Send<TRequest, TResponse>(TRequest request)
        {
            if (_processIo == null)
                throw new Exception("OmniSharp Server is not started.");
            
            await _processIo.StandardInput.WriteLineAsync(JsonConvert.SerializeObject(request));
            return null;
        }
    }
}