using Microsoft.Extensions.Logging;

namespace NetPad.BackgroundServices;

public abstract class BackgroundService :  Microsoft.Extensions.Hosting.BackgroundService
{
    protected readonly ILogger _logger;

    protected BackgroundService(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger(GetType());
    }

    public sealed override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting service");

        try
        {
            await StartingAsync(cancellationToken);

            await base.StartAsync(cancellationToken);

            _logger.LogDebug("Service started");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failure while starting service");
        }
    }

    public sealed override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Stopping service");

        try
        {
            await StoppingAsync(cancellationToken);

            await base.StopAsync(cancellationToken);

            _logger.LogDebug("Service stopped");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failure while stopping service");
        }
    }

    protected virtual Task StartingAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual Task StoppingAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
