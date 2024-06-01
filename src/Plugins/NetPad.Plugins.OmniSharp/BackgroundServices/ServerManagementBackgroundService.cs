using NetPad.Events;
using NetPad.Scripts;
using NetPad.Scripts.Events;
using NetPad.Sessions.Events;
using ISession = NetPad.Sessions.ISession;

namespace NetPad.Plugins.OmniSharp.BackgroundServices;

public class ServerManagementBackgroundService(
    OmniSharpServerCatalog serverCatalog,
    ISession session,
    IEventBus eventBus,
    ILogger<ServerManagementBackgroundService> logger)
    : IHostedService
{
    private readonly List<IDisposable> _disposables = [];

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var activeEnvChangedSubscription = eventBus.Subscribe<ActiveEnvironmentChangedEvent>(ev =>
        {
            var activatedEnvironmentScriptId = ev.ScriptId;

            if (activatedEnvironmentScriptId == null)
            {
                return Task.CompletedTask;
            }

            if (serverCatalog.HasOmniSharpServer(activatedEnvironmentScriptId.Value))
            {
                return Task.CompletedTask;
            }

            var environment = session.Get(activatedEnvironmentScriptId.Value);
            if (environment != null && environment.Script.Config.Kind != ScriptKind.SQL)
            {
                try
                {
                    // We don't want to wait for this, we want it to occur asynchronously
#pragma warning disable CS4014
                    serverCatalog.StartOmniSharpServerAsync(environment);
#pragma warning restore CS4014
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error occurred starting OmniSharp server for script {Script}", environment.Script);
                }
            }

            return Task.CompletedTask;
        });

        _disposables.Add(activeEnvChangedSubscription);


        var envRemovedSubscription = eventBus.Subscribe<EnvironmentsRemovedEvent>(ev =>
        {
            foreach (var environment in ev.Environments)
            {
                if (environment.Script.Config.Kind != ScriptKind.SQL)
                {
                    // We don't want to wait for this
#pragma warning disable CS4014
                    serverCatalog.StopOmniSharpServerAsync(environment);
#pragma warning restore CS4014
                }
            }

            return Task.CompletedTask;
        });

        _disposables.Add(envRemovedSubscription);

        var scriptKindChangedSubscription = eventBus.Subscribe<ScriptConfigPropertyChangedEvent>(async ev =>
        {
            if (ev.PropertyName != nameof(ScriptConfig.Kind)) return;

            await Task.Run(async () =>
            {
                try
                {
                    var newKind = (ScriptKind)ev.NewValue!;

                    var scriptEnvironment = session.Get(ev.ScriptId) ?? throw new Exception($"No script environment for script ID: {ev.ScriptId}");

                    if (newKind == ScriptKind.SQL)
                    {
                        if (serverCatalog.HasOmniSharpServer(scriptEnvironment.Script.Id))
                            await serverCatalog.StopOmniSharpServerAsync(scriptEnvironment);
                    }
                    else
                    {
                        await serverCatalog.StartOmniSharpServerAsync(scriptEnvironment);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error reacting to property change in ScriptConfig. Property: {PropertyName}. NewValue: {NewValue}",
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
