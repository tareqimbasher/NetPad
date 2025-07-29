using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NetPad.Apps;

public abstract class BackgroundService :  IHostedService
{
    protected readonly ILogger Logger;

    protected BackgroundService(ILoggerFactory loggerFactory)
    {
        Logger = loggerFactory.CreateLogger(GetType());
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogDebug("Starting service");

        try
        {
            await StartingAsync(cancellationToken);
            Logger.LogDebug("Service started");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failure starting service");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        Logger.LogDebug("Stopping service");

        try
        {
            await StoppingAsync(cancellationToken);
            Logger.LogDebug("Service stopped");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failure stopping service");
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
