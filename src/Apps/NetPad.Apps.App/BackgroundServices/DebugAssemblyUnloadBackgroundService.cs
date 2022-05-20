using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetPad.Utilities;

namespace NetPad.BackgroundServices;

/// <summary>
/// Periodically outputs the compiled script assemblies loaded by the program into memory. This is
/// mainly used to debug assembly unloading after script execution.
/// </summary>
public class DebugAssemblyUnloadBackgroundService : BackgroundService
{
    public DebugAssemblyUnloadBackgroundService(ILoggerFactory loggerFactory) : base(loggerFactory)
    {
    }

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
