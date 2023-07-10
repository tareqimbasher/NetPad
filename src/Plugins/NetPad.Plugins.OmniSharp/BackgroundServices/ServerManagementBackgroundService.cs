using NetPad.Events;
using NetPad.Scripts;
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
            if (environment != null && environment.Script.Config.Kind != ScriptKind.SQL)
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
                if (environment.Script.Config.Kind != ScriptKind.SQL)
                {
                    // We don't want to wait for this
#pragma warning disable CS4014
                    _serverCatalog.StopOmniSharpServerAsync(environment);
#pragma warning restore CS4014
                }
            }

            return Task.CompletedTask;
        });

        _disposables.Add(envRemovedSubscription);

        var scriptKindChangedSubscription = _eventBus.Subscribe<ScriptConfigPropertyChangedEvent>(async ev =>
        {
            if (ev.PropertyName != nameof(ScriptConfig.Kind)) return;

            await Task.Run(async () =>
            {
                try
                {
                    var newKind = (ScriptKind)ev.NewValue!;

                    var scriptEnvironment = _session.Get(ev.ScriptId) ?? throw new Exception($"No script environment for script ID: {ev.ScriptId}");

                    if (newKind == ScriptKind.SQL)
                    {
                        if (_serverCatalog.HasOmniSharpServer(scriptEnvironment.Script.Id))
                            await _serverCatalog.StopOmniSharpServerAsync(scriptEnvironment);
                    }
                    else
                    {
                        await _serverCatalog.StartOmniSharpServerAsync(scriptEnvironment);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reacting to property change in ScriptConfig. Property: {PropertyName}. NewValue: {NewValue}",
                        ev.PropertyName,
                        ev.NewValue);
                }
            });
        });

        _disposables.Add(scriptKindChangedSubscription);

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
