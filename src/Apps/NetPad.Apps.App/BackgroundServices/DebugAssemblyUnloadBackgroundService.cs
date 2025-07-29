using System.Linq;
using Microsoft.Extensions.Logging;
using NetPad.Apps;

namespace NetPad.BackgroundServices;

/// <summary>
/// Periodically outputs the compiled script assemblies loaded by the program into memory. This is
/// mainly used in development to debug assembly unloading after script execution (when using InMemoryScriptRunner).
/// </summary>
public class DebugAssemblyUnloadBackgroundService(ILoggerFactory loggerFactory) : BackgroundService(loggerFactory)
{
    protected override Task StartingAsync(CancellationToken stoppingToken)
    {
        Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(3000, stoppingToken);

                    int count = 0;
                    var names = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.FullName)
                        .Where(n => n?.Contains("NetPadScript") == true)
                        .Select(s => $"{++count}. {s?.Split(',')[0]}");

                    Logger.LogDebug("Loaded NetPad script assemblies (count: {Count}):\n{Assemblies}",
                        count,
                        names.JoinToString("\n"));
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Unhandled exception");
                }
            }
        });

        return Task.CompletedTask;
    }
}
