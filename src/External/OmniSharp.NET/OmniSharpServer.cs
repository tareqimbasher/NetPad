using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniSharp.Utilities;

namespace OmniSharp
{
    public abstract class OmniSharpServer<TConfiguration> : IOmniSharpServer
        where TConfiguration : OmniSharpServerConfiguration
    {
        private int _sequence;

        protected OmniSharpServer(TConfiguration configuration, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;
            Logger = loggerFactory.CreateLogger(GetType().FullName);
            _sequence = 100;
        }

        public TConfiguration Configuration { get; }
        protected ILogger Logger { get; }

        public abstract Task StartAsync();

        public abstract Task StopAsync();

        public abstract Task SendAsync(object request);
        public abstract Task<TResponse?> SendAsync<TResponse>(object request) where TResponse : class;
        public abstract Task SendAsync<TRequest>(IEnumerable<TRequest> requests);
        public abstract Task<TResponse?> SendAsync<TRequest, TResponse>(IEnumerable<TRequest> requests) where TResponse : class;
        public abstract Task<TResponse?> SendAsync<TResponse>(string endpointName, object request) where TResponse : class;

        protected int NextSequence() => ++_sequence;

        public virtual void Dispose()
        {
            AsyncHelpers.RunSync(StopAsync);
        }
    }
}
