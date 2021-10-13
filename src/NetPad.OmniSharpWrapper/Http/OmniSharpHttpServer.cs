using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetPad.OmniSharpWrapper.Utilities;

namespace NetPad.OmniSharpWrapper.Http
{
    public class OmniSharpHttpServer : OmniSharpServer<OmniSharpHttpServerConfiguration>
    {
        private readonly IOmniSharpServerProcessAccessor<string> _omniSharpServerProcessAccessor;
        private string? _uri;

        public OmniSharpHttpServer(
            OmniSharpHttpServerConfiguration configuration, 
            IOmniSharpServerProcessAccessor<string> omniSharpServerProcessAccessor,
            ILoggerFactory loggerFactory) :
            base(configuration, loggerFactory)
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

        public override async Task<TResponse> Send<TResponse>(object request)
        {
            throw new NotImplementedException();
        }
    }
}