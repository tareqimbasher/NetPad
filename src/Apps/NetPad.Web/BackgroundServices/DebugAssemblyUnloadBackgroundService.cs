using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetPad.Utilities;

namespace NetPad.BackgroundServices
{
    public class DebugAssemblyUnloadBackgroundService : BackgroundService
    {
        private readonly ILogger<DebugAssemblyUnloadBackgroundService> _logger;

        public DebugAssemblyUnloadBackgroundService(ILogger<DebugAssemblyUnloadBackgroundService> logger)
        {
            _logger = logger;
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
                        .Select(s => $"{++count}. {s?.Split(',')[0]}")
                        .JoinToString("\n");

                    _logger.LogDebug($"Loaded NetPad script assemblies:\n{names}");
                }
            });

            return Task.CompletedTask;
        }
    }
}
