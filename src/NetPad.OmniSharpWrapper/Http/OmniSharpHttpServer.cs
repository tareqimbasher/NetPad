using System;
using System.Threading.Tasks;
using NetPad.OmniSharpWrapper.Utilities;

namespace NetPad.OmniSharpWrapper.Http
{
    public class OmniSharpHttpServer : OmniSharpServer<OmniSharpHttpServerConfiguration>
    {
        private readonly IOmniSharpServerProcessAccessor<string> _omniSharpServerProcessAccessor;
        private string? _uri;

        public OmniSharpHttpServer(OmniSharpHttpServerConfiguration configuration, IOmniSharpServerProcessAccessor<string> omniSharpServerProcessAccessor) :
            base(configuration)
        {
            _omniSharpServerProcessAccessor = omniSharpServerProcessAccessor;
        }

        public override async Task StartAsync()
        {
            _uri = await _omniSharpServerProcessAccessor.GetEntryPointAsync();
        }

        public override async Task StopAsync()
        {
            await _omniSharpServerProcessAccessor.StopProcessAsync();
        }

        public override async Task<TResponse> Send<TRequest, TResponse>(TRequest request)
        {
            throw new NotImplementedException();
        }
    }
}