using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetPad.Application;
using NetPad.Configuration;
using NetPad.Plugins;
using NetPad.Scripts;
using NetPad.Sessions;

namespace NetPad.BackgroundServices;

public class AppSetupAndCleanupBackgroundService : BackgroundService
{
    private readonly ISession _session;
    private readonly IAutoSaveScriptRepository _autoSaveScriptRepository;
    private readonly IPluginManager _pluginManager;
    private readonly IAppStatusMessagePublisher _appStatusMessagePublisher;

    public AppSetupAndCleanupBackgroundService(
        ISession session,
        IAutoSaveScriptRepository autoSaveScriptRepository,
        IPluginManager pluginManager,
        IAppStatusMessagePublisher appStatusMessagePublisher,
        ILoggerFactory loggerFactory) : base(loggerFactory)
    {
        _session = session;
        _autoSaveScriptRepository = autoSaveScriptRepository;
        _pluginManager = pluginManager;
        _appStatusMessagePublisher = appStatusMessagePublisher;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var autoSavedScripts = await _autoSaveScriptRepository.GetScriptsAsync();

        await _session.OpenAsync(autoSavedScripts);
    }

    protected override async Task StoppingAsync(CancellationToken cancellationToken)
    {
        await _appStatusMessagePublisher.PublishAsync("Stopping...", AppStatusMessagePriority.Normal, true);

        var environments = _session.Environments;

        await _session.CloseAsync(environments.Select(e => e.Script.Id).ToArray());

        AppDataProvider.ExternalProcessesDirectoryPath.DeleteIfExists();
        AppDataProvider.TypedDataContextTempDirectoryPath.DeleteIfExists();

        foreach (var registration in _pluginManager.PluginRegistrations)
        {
            await registration.Plugin.CleaupAsync();
        }
    }
}
