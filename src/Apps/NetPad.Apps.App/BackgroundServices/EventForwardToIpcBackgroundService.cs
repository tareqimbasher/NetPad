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

    public EventForwardToIpcBackgroundService(IEventBus eventBus, IIpcService ipcService, ILoggerFactory loggerFactory) : base(loggerFactory)
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
        SubscribeAndForwardToIpc<ActiveEnvironmentChanged>();
        SubscribeAndForwardToIpc<EnvironmentsAdded>();
        SubscribeAndForwardToIpc<EnvironmentsRemoved>();
        SubscribeAndForwardToIpc<SettingsUpdated>();
        SubscribeAndForwardToIpc<AppStatusMessagePublished>();
    }

    private void ForwardEnvironmentLevelEvents()
    {
        _eventBus.Subscribe<EnvironmentsAdded>(ev =>
        {
            foreach (var environment in ev.Environments)
            {
                SubscribeAndForwardToIpc<EnvironmentPropertyChanged>(environment, e => e.ScriptId == environment.Script.Id);
                SubscribeAndForwardToIpc<ScriptPropertyChanged>(environment,
                    e => e.ScriptId == environment.Script.Id && e.PropertyName != nameof(Script.Code));
                SubscribeAndForwardToIpc<ScriptConfigPropertyChanged>(environment, e => e.ScriptId == environment.Script.Id);
            }

            return Task.CompletedTask;
        });

        _eventBus.Subscribe<EnvironmentsRemoved>(ev =>
        {
            Unsubscribe(ev.Environments);
            return Task.CompletedTask;
        });
    }

    private void SubscribeAndForwardToIpc<TEvent>() where TEvent : class, IEvent
    {
        _eventBus.Subscribe<TEvent>(async ev => { await _ipcService.SendAsync(ev); });
    }

    private void SubscribeAndForwardToIpc<TEvent>(ScriptEnvironment environment, Func<TEvent, bool>? predicate = null) where TEvent : class, IScriptEvent
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
