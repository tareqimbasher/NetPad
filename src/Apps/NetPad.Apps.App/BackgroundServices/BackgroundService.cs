using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NetPad.BackgroundServices
{
    public abstract class BackgroundService :  Microsoft.Extensions.Hosting.BackgroundService
    {
        protected readonly ILogger _logger;

        protected BackgroundService(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(GetType());
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Starting background service");

            await base.StartAsync(cancellationToken);

            _logger.LogDebug("Background service started");
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogDebug("Stopping background service");

            await base.StopAsync(cancellationToken);

            _logger.LogDebug("Background service stopped");
        }
    }
}
