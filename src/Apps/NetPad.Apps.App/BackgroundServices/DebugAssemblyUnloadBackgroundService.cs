using System.Linq;
using Microsoft.Extensions.Logging;

namespace NetPad.BackgroundServices;

/// <summary>
/// Periodically outputs the compiled script assemblies loaded by the program into memory. This is
/// mainly used to debug assembly unloading after script execution (when using InMemoryScriptRunner only).
/// </summary>
public class DebugAssemblyUnloadBackgroundService(ILoggerFactory loggerFactory) : BackgroundService(loggerFactory)
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(3000);

                int count = 0;
                var names = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.FullName)
                    .Where(n => n?.Contains("NetPadScript") == true)
                    .Select(s => $"{++count}. {s?.Split(',')[0]}");

                _logger.LogDebug("Loaded NetPad script assemblies (count: {Count}):\n{Assemblies}",
                    count,
                    names.JoinToString("\n"));
            }
        });

        return Task.CompletedTask;
    }
}
