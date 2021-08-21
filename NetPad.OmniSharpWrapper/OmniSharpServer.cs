using System.Diagnostics;
using System.Threading.Tasks;
using NetPad.OmniSharpWrapper.Utilities;
using OmniSharp.Models;
using OmniSharp.Models.V2.GotoDefinition;

namespace NetPad.OmniSharpWrapper
{
    public abstract class OmniSharpServer<TConfiguration> : IOmniSharpServer
        where TConfiguration : OmniSharpServerConfiguration
    {
        public OmniSharpServer(TConfiguration configuration)
        {
            Configuration = configuration;
        }
        
        public TConfiguration Configuration { get; }

        public abstract Task StartAsync();

        public abstract Task StopAsync();

        public abstract Task<TResponse> Send<TRequest, TResponse>(TRequest request)
            where TRequest : Request
            where TResponse : class;
        

        private async Task Test()
        {
            var response = await Send<GotoDefinitionRequest, object>(new GotoDefinitionRequest());
        }

        public virtual void Dispose()
        {
            AsyncHelpers.RunSync(StopAsync);
        }
    }
}