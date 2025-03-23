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
            if (environment == null || environment.Script.Config.Kind == ScriptKind.SQL)
            {
                return Task.CompletedTask;
            }

            Task.Run(async () =>
            {
                try
                {
                    // If the user has already switched to a different tab less than a second later they might
                    // just be quickly browsing, don't start OmniSharp just yet.
                    await Task.Delay(1000, cancellationToken);
                    if (environment != session.Active)
                    {
                        return;
                    }

                    _ = serverCatalog.StartOmniSharpServerAsync(environment);
                }
                catch (TaskCanceledException)
                {
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error occurred starting OmniSharp server for script {Script}",
                        environment.Script);
                }
            });

            return Task.CompletedTask;
        });

        _disposables.Add(activeEnvChangedSubscription);


        var envRemovedSubscription = eventBus.Subscribe<EnvironmentsRemovedEvent>(ev =>
        {
            foreach (var environment in ev.Environments)
            {
                if (environment.Script.Config.Kind != ScriptKind.SQL)
                {
                    _ = serverCatalog.StopOmniSharpServerAsync(environment);
                }
            }

            return Task.CompletedTask;
        });

        _disposables.Add(envRemovedSubscription);

        var scriptKindChangedSubscription = eventBus.Subscribe<ScriptConfigPropertyChangedEvent>(ev =>
        {
            if (ev.PropertyName != nameof(ScriptConfig.Kind))
            {
                return Task.CompletedTask;
            }

            try
            {
                var newKind = (ScriptKind)ev.NewValue!;

                var scriptEnvironment = session.Get(ev.ScriptId) ??
                                        throw new Exception($"No script environment for script ID: {ev.ScriptId}");

                if (newKind == ScriptKind.SQL)
                {
                    if (serverCatalog.HasOmniSharpServer(scriptEnvironment.Script.Id))
                        _ = serverCatalog.StopOmniSharpServerAsync(scriptEnvironment);
                }
                else
                {
                    _ = serverCatalog.StartOmniSharpServerAsync(scriptEnvironment);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Error reacting to property change in ScriptConfig. Property: {PropertyName}. NewValue: {NewValue}",
                    ev.PropertyName,
                    ev.NewValue);
            }

            return Task.CompletedTask;
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
