using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetPad.Events;
using NetPad.Scripts;
using NetPad.UiInterop;

namespace NetPad.BackgroundServices;

/// <summary>
/// Forwards specific events to IPC clients.
/// </summary>
public class EventForwardToIpcBackgroundService : BackgroundService
{
    private readonly IEventBus _eventBus;
    private readonly IIpcService _ipcService;
    private readonly Dictionary<Guid, List<EventSubscriptionToken>> _environmentSubscriptionTokens;

    public EventForwardToIpcBackgroundService(IEventBus eventBus, IIpcService ipcService, ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
        _eventBus = eventBus;
        _ipcService = ipcService;
        _environmentSubscriptionTokens = new Dictionary<Guid, List<EventSubscriptionToken>>();
    }

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
    }

    private void ForwardEnvironmentLevelEvents()
    {
        _eventBus.Subscribe<EnvironmentsAddedEvent>(ev =>
        {
            foreach (var environment in ev.Environments)
            {
                SubscribeAndForwardToIpc<EnvironmentPropertyChangedEvent>(environment,
                    e => e.ScriptId == environment.Script.Id);
                SubscribeAndForwardToIpc<ScriptPropertyChangedEvent>(environment,
                    e => e.ScriptId == environment.Script.Id
                         // HACK: Script code could potentially be large (ex. a large base 64 string is used in code)
                         // which could cause race conditions where 2 subsequent push notifications could be sent
                         // and received on clients' out of order. While we can solve this with verifying message
                         // order, since this is the only case currently where we need to worry about it, opting to
                         // just have the FE handle notifying itself when script code changes.
                         //
                         // This also means that if a script's code is changed outside of that client, the client will
                         // not know about it. This is an area we'd want to change to support of live code changes from
                         // multiple sources (ex. multiple users working on the same script)
                         && e.PropertyName != nameof(Script.Code)
                );
                SubscribeAndForwardToIpc<ScriptConfigPropertyChangedEvent>(environment,
                    e => e.ScriptId == environment.Script.Id);
            }

            return Task.CompletedTask;
        });

        _eventBus.Subscribe<EnvironmentsRemovedEvent>(ev =>
        {
            Unsubscribe(ev.Environments);
            return Task.CompletedTask;
        });
    }

    private void SubscribeAndForwardToIpc<TEvent>() where TEvent : class, IEvent
    {
        _eventBus.Subscribe<TEvent>(async ev => { await _ipcService.SendAsync(ev); });
    }

    private void SubscribeAndForwardToIpc<TEvent>(ScriptEnvironment environment, Func<TEvent, bool>? predicate = null)
        where TEvent : class, IScriptEvent
    {
        var token = _eventBus.Subscribe<TEvent>(async ev =>
        {
            if (predicate != null && !predicate(ev))
            {
                return;
            }

            await _ipcService.SendAsync(ev);
        });

        AddEnvironmentEventToken(environment, token);
    }

    private void AddEnvironmentEventToken(ScriptEnvironment environment, EventSubscriptionToken token)
    {
        if (!_environmentSubscriptionTokens.TryGetValue(environment.Script.Id, out var tokens))
        {
            tokens = new List<EventSubscriptionToken>();
            _environmentSubscriptionTokens.Add(environment.Script.Id, tokens);
        }

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
                _eventBus.Unsubscribe(token);
            }

            _environmentSubscriptionTokens.Remove(environment.Script.Id);
        }
    }
}
