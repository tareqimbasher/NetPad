using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetPad.Scripts;
using NetPad.Sessions;

namespace NetPad.BackgroundServices;

public class AppSetupAndCleanupBackgroundService : BackgroundService
{
    private readonly ISession _session;
    private readonly IAutoSaveScriptRepository _autoSaveScriptRepository;

    public AppSetupAndCleanupBackgroundService(
        ISession session,
        IAutoSaveScriptRepository autoSaveScriptRepository,
        ILoggerFactory loggerFactory) : base(loggerFactory)
    {
        _session = session;
        _autoSaveScriptRepository = autoSaveScriptRepository;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var autoSavedScripts = await _autoSaveScriptRepository.GetScriptsAsync();

        await _session.OpenAsync(autoSavedScripts);
    }

    protected override async Task StoppingAsync(CancellationToken cancellationToken)
    {
        var environments = _session.Environments;

        await _session.CloseAsync(environments.Select(e => e.Script.Id).ToArray());
    }
}
