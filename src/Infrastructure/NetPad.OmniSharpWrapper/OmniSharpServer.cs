using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetPad.OmniSharpWrapper.Utilities;
using OmniSharp.Models;
using OmniSharp.Models.V2.GotoDefinition;

namespace NetPad.OmniSharpWrapper
{
    public abstract class OmniSharpServer<TConfiguration> : IOmniSharpServer
        where TConfiguration : OmniSharpServerConfiguration
    {
        public OmniSharpServer(TConfiguration configuration, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;
            Logger = loggerFactory.CreateLogger("OmniSharpServer");
            Sequence = 1000;
        }
        
        public TConfiguration Configuration { get; }
        protected ILogger Logger { get; }
        protected int Sequence { get; set; }
        
        public abstract Task StartAsync();

        public abstract Task StopAsync();

        public abstract Task<TResponse> Send<TResponse>(object request) where TResponse : class;
        

        public virtual void Dispose()
        {
            AsyncHelpers.RunSync(StopAsync);
        }
    }
}