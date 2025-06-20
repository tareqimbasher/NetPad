using System.Linq;
using Microsoft.Extensions.Logging;
using NetPad.Application;
using NetPad.Apps.Plugins;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.Scripts;
using NetPad.Sessions;

namespace NetPad.BackgroundServices;

public class AppSetupAndCleanupBackgroundService(
    ISession session,
    IScriptRepository scriptRepository,
    IAutoSaveScriptRepository autoSaveScriptRepository,
    ITrivialDataStore trivialDataStore,
    IPluginManager pluginManager,
    IAppStatusMessagePublisher appStatusMessagePublisher,
    ILoggerFactory loggerFactory)
    : BackgroundService(loggerFactory)
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Open auto-saved scripts
        var autoSavedScripts = await autoSaveScriptRepository.GetScriptsAsync();
        await session.OpenAsync(autoSavedScripts, false);

        try
        {
            // Open scripts from the previous session
            var previousSessionOpenScriptIds = trivialDataStore.Get<Guid[]>(Session.OpenScriptsSaveKey)?
                .Where(sid => !session.IsOpen(sid))
                .ToHashSet();

            if (previousSessionOpenScriptIds?.Count > 0)
            {
                var scripts = await scriptRepository.GetAsync(previousSessionOpenScriptIds);
                await session.OpenAsync(scripts, false);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error opening scripts from previous session");
        }

        await session.ActivateBestCandidateAsync();
    }

    protected override async Task StoppingAsync(CancellationToken cancellationToken)
    {
        _ = appStatusMessagePublisher.PublishAsync("Closing...", AppStatusMessagePriority.Normal, true);

        var environments = session.GetOpened();

        try
        {
            var scriptIds = environments.Select(e => e.Script.Id).ToArray();
            await session.CloseAsync(scriptIds, false, false);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error closing environments");
        }

        Try.Run(() => AppDataProvider.ExternalProcessesDirectoryPath.DeleteIfExists());
        Try.Run(() => AppDataProvider.TypedDataContextTempDirectoryPath.DeleteIfExists());

        foreach (var registration in pluginManager.PluginRegistrations)
        {
            try
            {
                await registration.Plugin.CleanupAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error cleaning up plugin: {PluginName} ({PluginId})",
                    registration.Plugin.Name,
                    registration.Plugin.Id);
            }
        }
    }
}
