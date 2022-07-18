using NetPad.Configuration;
using NetPad.Events;
using NetPad.Plugins.OmniSharp.Services;
using ISession = NetPad.Sessions.ISession;

namespace NetPad.Plugins.OmniSharp.BackgroundServices;

public class ServerManagementBackgroundService : BackgroundService
{
    private readonly OmniSharpServerCatalog _serverCatalog;
    private readonly ISession _session;
    private readonly Settings _settings;
    private readonly IEventBus _eventBus;
    private readonly ILogger<ServerManagementBackgroundService> _logger;

    public ServerManagementBackgroundService(
        OmniSharpServerCatalog serverCatalog,
        ISession session,
        Settings settings,
        IEventBus eventBus,
        ILogger<ServerManagementBackgroundService> logger)
    {
        _serverCatalog = serverCatalog;
        _session = session;
        _settings = settings;
        _eventBus = eventBus;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _eventBus.Subscribe<ActiveEnvironmentChangedEvent>(ev =>
        {
            var activatedEnvironmentScriptId = ev.ScriptId;

            if (activatedEnvironmentScriptId == null || !_settings.EditorOptions.CodeCompletion.Enabled)
            {
                return Task.CompletedTask;
            }

            if (_serverCatalog.HasOmniSharpServer(activatedEnvironmentScriptId.Value))
            {
                return Task.CompletedTask;
            }

            var environment = _session.Get(activatedEnvironmentScriptId.Value);
            if (environment != null)
            {
                try
                {
                    // We don't want to wait for this, we want it to occur asynchronously
#pragma warning disable CS4014
                    _serverCatalog.StartOmniSharpServerAsync(environment);
#pragma warning restore CS4014
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred starting OmniSharp server for script {Script}", environment.Script);
                }
            }

            return Task.CompletedTask;
        });

        _eventBus.Subscribe<EnvironmentsRemovedEvent>(ev =>
        {
            foreach (var environment in ev.Environments)
            {
                // We don't want to wait for this
#pragma warning disable CS4014
                _serverCatalog.StopOmniSharpServerAsync(environment);
#pragma warning restore CS4014
            }

            return Task.CompletedTask;
        });


        return Task.CompletedTask;
    }
}
