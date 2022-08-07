using NetPad.Events;
using ISession = NetPad.Sessions.ISession;

namespace NetPad.Plugins.OmniSharp.BackgroundServices;

public class ServerManagementBackgroundService : IHostedService
{
    private readonly OmniSharpServerCatalog _serverCatalog;
    private readonly ISession _session;
    private readonly IEventBus _eventBus;
    private readonly ILogger<ServerManagementBackgroundService> _logger;
    private readonly List<IDisposable> _disposables;

    public ServerManagementBackgroundService(
        OmniSharpServerCatalog serverCatalog,
        ISession session,
        IEventBus eventBus,
        ILogger<ServerManagementBackgroundService> logger)
    {
        _serverCatalog = serverCatalog;
        _session = session;
        _eventBus = eventBus;
        _logger = logger;
        _disposables = new List<IDisposable>();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var activeEnvChangedSubscription = _eventBus.Subscribe<ActiveEnvironmentChangedEvent>(ev =>
        {
            var activatedEnvironmentScriptId = ev.ScriptId;

            if (activatedEnvironmentScriptId == null)
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

        _disposables.Add(activeEnvChangedSubscription);


        var envRemovedSubscription = _eventBus.Subscribe<EnvironmentsRemovedEvent>(ev =>
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

        _disposables.Add(envRemovedSubscription);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }

        return Task.CompletedTask;
    }
}
