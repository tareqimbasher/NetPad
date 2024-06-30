using System.Linq;
using Microsoft.Extensions.Logging;
using NetPad.Application;
using NetPad.Apps.Plugins;
using NetPad.Configuration;
using NetPad.Scripts;
using NetPad.Sessions;

namespace NetPad.BackgroundServices;

public class AppSetupAndCleanupBackgroundService(
    ISession session,
    IAutoSaveScriptRepository autoSaveScriptRepository,
    IPluginManager pluginManager,
    IAppStatusMessagePublisher appStatusMessagePublisher,
    ILoggerFactory loggerFactory)
    : BackgroundService(loggerFactory)
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var autoSavedScripts = await autoSaveScriptRepository.GetScriptsAsync();

        await session.OpenAsync(autoSavedScripts);
    }

    protected override async Task StoppingAsync(CancellationToken cancellationToken)
    {
        await appStatusMessagePublisher.PublishAsync("Stopping...", AppStatusMessagePriority.Normal, true);

        var environments = session.Environments;

        await session.CloseAsync(environments.Select(e => e.Script.Id).ToArray());

        AppDataProvider.ExternalProcessesDirectoryPath.DeleteIfExists();
        AppDataProvider.TypedDataContextTempDirectoryPath.DeleteIfExists();

        foreach (var registration in pluginManager.PluginRegistrations)
        {
            await registration.Plugin.CleaupAsync();
        }
    }
}
