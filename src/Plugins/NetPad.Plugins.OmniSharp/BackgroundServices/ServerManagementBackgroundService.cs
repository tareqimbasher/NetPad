using NetPad.Apps;
using NetPad.Events;
using NetPad.Scripts;
using NetPad.Scripts.Events;
using NetPad.Sessions.Events;
using ISession = NetPad.Sessions.ISession;

namespace NetPad.Plugins.OmniSharp.BackgroundServices;

/// <summary>
/// Starts and stops OmniSharp server instances in response to session changes.
/// </summary>
public class ServerManagementBackgroundService(
    OmniSharpServerCatalog serverCatalog,
    ISession session,
    IEventBus eventBus,
    ILoggerFactory loggerFactory)
    : BackgroundService(loggerFactory)
{
    private readonly List<IDisposable> _disposables = [];

    protected override Task StartingAsync(CancellationToken cancellationToken)
    {
        // When a script environment is activated, start a OmniSharp server instance for it.
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
                    Logger.LogError(ex, "Error occurred starting OmniSharp server for script {Script}",
                        environment.Script);
                }
            });

            return Task.CompletedTask;
        });

        _disposables.Add(activeEnvChangedSubscription);

        // When a script environment is closed/removed, stop its related OmniSharp instance.
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

        // Only have an OmniSharp server instance running if the script is not of Kind = SQL
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
                else if (!serverCatalog.HasOmniSharpServer(scriptEnvironment.Script.Id))
                {
                    _ = serverCatalog.StartOmniSharpServerAsync(scriptEnvironment);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex,
                    "Error reacting to property change in ScriptConfig. Property: {PropertyName}. NewValue: {NewValue}",
                    ev.PropertyName,
                    ev.NewValue);
            }

            return Task.CompletedTask;
        });

        _disposables.Add(scriptKindChangedSubscription);

        return Task.CompletedTask;
    }

    protected override Task StoppingAsync(CancellationToken cancellationToken)
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }

        return Task.CompletedTask;
    }
}
