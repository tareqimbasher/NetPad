using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NetPad.Application.Events;
using NetPad.Apps.UiInterop;
using NetPad.Configuration.Events;
using NetPad.Data.Events;
using NetPad.Events;
using NetPad.Scripts;
using NetPad.Scripts.Events;
using NetPad.Sessions.Events;

namespace NetPad.BackgroundServices;

/// <summary>
/// Forwards some event bus messages to IPC clients.
/// </summary>
public class EventForwardToIpcBackgroundService(
    IEventBus eventBus,
    IIpcService ipcService,
    ILoggerFactory loggerFactory)
    : BackgroundService(loggerFactory)
{
    private readonly ConcurrentDictionary<Guid, List<EventSubscriptionToken>> _environmentSubscriptionTokens = new();

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
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
                         //
                         // Script code can potentially be large (ex. a large base64 string is used in code)
                         // which could cause race conditions where 2 subsequent push notifications could be sent
                         // and received on clients out of order. While we can solve this by sending message
                         // order and verifying it on the client, that is too much work since this is the only case
                         // currently where we need to worry about it. So we're opting to not notify when code changes
                         // and just have the client handle notifying itself when script code changes.
                         //
                         // This also means that if a script's code is changed outside of that client (ex. on the
                         // backend), the client will not know about it. This is something we'd want to change if we
                         // want support live code changes from multiple sources (ex. multiple users working on the
                         // same script at the same time).
                         && e.PropertyName != nameof(Script.Code)
                );
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
    /// <param name="environment"></param>
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
