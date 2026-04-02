using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NetPad.Application.Events;
using NetPad.Apps;
using NetPad.Apps.UiInterop;
using NetPad.Configuration.Events;
using NetPad.Data.Events;
using NetPad.Events;
using NetPad.Scripts;
using NetPad.Scripts.Events;
using NetPad.Sessions.Events;

namespace NetPad.BackgroundServices;

/// <summary>
/// Forwards certain EventBus messages that are produced by this app (the host) to IPC clients.
/// </summary>
public class EventForwardToIpcBackgroundService(
    IEventBus eventBus,
    IIpcService ipcService,
    ILoggerFactory loggerFactory)
    : BackgroundService(loggerFactory)
{
    private readonly ConcurrentDictionary<Guid, List<EventSubscriptionToken>> _environmentSubscriptionTokens = new();

    protected override Task StartingAsync(CancellationToken stoppingToken)
    {
        ForwardApplicationLevelEvents();
        ForwardEnvironmentLevelEvents();

        return Task.CompletedTask;
    }

    private void ForwardApplicationLevelEvents()
    {
        SubscribeAndForwardToIpc<ActiveEnvironmentChangedEvent>();
        SubscribeAndForwardToIpc<EnvironmentsAddedEvent>();
        SubscribeAndForwardToIpc<EnvironmentsRemovedEvent>();
        SubscribeAndForwardToIpc<SettingsUpdatedEvent>();
        SubscribeAndForwardToIpc<AppStatusMessagePublishedEvent>();
        SubscribeAndForwardToIpc<DataConnectionSavedEvent>();
        SubscribeAndForwardToIpc<DataConnectionDeletedEvent>();
        SubscribeAndForwardToIpc<DatabaseServerSavedEvent>();
        SubscribeAndForwardToIpc<DatabaseServerDeletedEvent>();
        SubscribeAndForwardToIpc<DataConnectionResourcesUpdatingEvent>();
        SubscribeAndForwardToIpc<DataConnectionResourcesUpdatedEvent>();
        SubscribeAndForwardToIpc<DataConnectionResourcesUpdateFailedEvent>();
        SubscribeAndForwardToIpc<DataConnectionSchemaValidationStartedEvent>();
        SubscribeAndForwardToIpc<DataConnectionSchemaValidationCompletedEvent>();
    }

    private void ForwardEnvironmentLevelEvents()
    {
        eventBus.Subscribe<EnvironmentsAddedEvent>(ev =>
        {
            foreach (var environment in ev.Environments)
            {
                SubscribeAndForwardToIpc<EnvironmentPropertyChangedEvent>(environment,
                    e => e.ScriptId == environment.Script.Id);
                SubscribeAndForwardToIpc<ScriptPropertyChangedEvent>(environment,
                    e => e.ScriptId == environment.Script.Id
                         // Do not forward event if the Script.Code property has changed.
                         // Script code can potentially be large (ex. a large base64 string is used in code)
                         // Code updates are forwarded using a dedicated event: ScriptCodeUpdatedEvent
                         && e.PropertyName != nameof(Script.Code)
                );
                SubscribeAndForwardToIpc<ScriptCodeUpdatedEvent>(environment,
                    e => e.ScriptId == environment.Script.Id && e.ExternallyInitiated);
                SubscribeAndForwardToIpc<ScriptConfigPropertyChangedEvent>(environment,
                    e => e.ScriptId == environment.Script.Id);
            }

            return Task.CompletedTask;
        });

        eventBus.Subscribe<EnvironmentsRemovedEvent>(ev =>
        {
            Unsubscribe(ev.Environments);
            return Task.CompletedTask;
        });
    }

    private void SubscribeAndForwardToIpc<TEvent>() where TEvent : class, IEvent
    {
        eventBus.Subscribe<TEvent>(async ev => { await ipcService.SendAsync(ev); });
    }

    /// <summary>
    /// Subscribes to an event that is fired in this application and forwards it to connected clients.
    /// </summary>
    /// <param name="environment">The script environment the event is related to.</param>
    /// <param name="predicate">If specified, event will only be forwarded if this predicate returns true.</param>
    private void SubscribeAndForwardToIpc<TEvent>(ScriptEnvironment environment, Func<TEvent, bool>? predicate = null)
        where TEvent : class, IScriptEvent
    {
        var token = eventBus.Subscribe<TEvent>(async ev =>
        {
            if (predicate != null && !predicate(ev))
            {
                return;
            }

            await ipcService.SendAsync(ev);
        });

        AddEnvironmentEventToken(environment, token);
    }

    private void AddEnvironmentEventToken(ScriptEnvironment environment, EventSubscriptionToken token)
    {
        var tokens = _environmentSubscriptionTokens.GetOrAdd(environment.Script.Id, static _ => []);
        tokens.Add(token);
    }

    private void Unsubscribe(ScriptEnvironment[] environments)
    {
        foreach (var environment in environments)
        {
            if (!_environmentSubscriptionTokens.TryGetValue(environment.Script.Id, out var tokens))
            {
                continue;
            }

            foreach (var token in tokens)
            {
                eventBus.Unsubscribe(token);
            }

            _environmentSubscriptionTokens.TryRemove(environment.Script.Id, out _);
        }
    }
}
