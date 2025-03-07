﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OmniSharp.Utilities;

namespace OmniSharp
{
    internal abstract class OmniSharpServer<TConfiguration> : IOmniSharpServer
        where TConfiguration : OmniSharpServerConfiguration
    {
        protected readonly ILoggerFactory _loggerFactory;
        private int _sequence;

        protected OmniSharpServer(TConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
            Configuration = configuration;
            Logger = loggerFactory.CreateLogger(GetType().FullName ?? GetType().Name);
            _sequence = 100;
        }

        public TConfiguration Configuration { get; }
        protected ILogger Logger { get; }

        public abstract Task StartAsync();

        public abstract Task StopAsync();

        public abstract Task SendAsync(object request, CancellationToken? cancellationToken = default);
        public abstract Task<TResponse?> SendAsync<TResponse>(object request, CancellationToken? cancellationToken = default) where TResponse : class;
        public abstract Task SendAsync<TRequest>(IEnumerable<TRequest> requests, CancellationToken? cancellationToken = default);

        public abstract Task<TResponse?> SendAsync<TRequest, TResponse>(IEnumerable<TRequest> requests, CancellationToken? cancellationToken = default)
            where TResponse : class;

        public abstract Task<TResponse?> SendAsync<TResponse>(string endpointName, object request, CancellationToken? cancellationToken = default)
            where TResponse : class;

        protected int NextSequence()
        {
            return Interlocked.Increment(ref _sequence);
        }

        public virtual void Dispose()
        {
            AsyncHelpers.RunSync(StopAsync);
        }
    }
}
