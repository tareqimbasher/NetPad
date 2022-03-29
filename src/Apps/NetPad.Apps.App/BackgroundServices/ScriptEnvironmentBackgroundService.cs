using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetPad.Events;
using NetPad.IO;
using NetPad.Scripts;
using NetPad.Services;
using NetPad.UiInterop;

namespace NetPad.BackgroundServices;

public class ScriptEnvironmentBackgroundService : BackgroundService
{
    private readonly IEventBus _eventBus;
    private readonly IIpcService _ipcService;
    private readonly IAutoSaveScriptRepository _autoSaveScriptRepository;

    private readonly Dictionary<Guid, List<EventSubscriptionToken>> _environmentSubscriptionTokens;

    public ScriptEnvironmentBackgroundService(IEventBus eventBus, IIpcService ipcService, IAutoSaveScriptRepository autoSaveScriptRepository)
    {
        _eventBus = eventBus;
        _ipcService = ipcService;
        _autoSaveScriptRepository = autoSaveScriptRepository;
        _environmentSubscriptionTokens = new Dictionary<Guid, List<EventSubscriptionToken>>();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ListenToEnvironmentsChanges();

        return Task.CompletedTask;
    }

    private void ListenToEnvironmentsChanges()
    {
        _eventBus.Subscribe<EnvironmentsAdded>(ev =>
        {
            foreach (var environment in ev.Environments)
            {
                ForwardRelevantEventsToIpc(environment);
                AutoSaveScriptChanges(environment);

                environment.SetIO(ActionInputReader.Null, new IpcScriptOutputWriter(environment, _ipcService));
            }

            return Task.CompletedTask;
        });

        _eventBus.Subscribe<EnvironmentsRemoved>(ev =>
        {
            foreach (var environment in ev.Environments)
            {
                if (!_environmentSubscriptionTokens.TryGetValue(environment.Script.Id, out var tokens)) continue;

                foreach (var token in tokens)
                {
                    _eventBus.Unsubscribe(token);
                }

                _environmentSubscriptionTokens.Remove(environment.Script.Id);
            }

            return Task.CompletedTask;
        });
    }

    private void AutoSaveScriptChanges(ScriptEnvironment environment)
    {
        var scriptPropChangeToken = _eventBus.Subscribe<ScriptPropertyChanged>(async ev =>
        {
            if (ev.ScriptId != environment.Script.Id)
                return;

            await _autoSaveScriptRepository.SaveAsync(environment.Script);
        });

        AddEnvironmentEventToken(environment, scriptPropChangeToken);

        var scriptConfigPropChangeToken = _eventBus.Subscribe<ScriptConfigPropertyChanged>(async ev =>
        {
            if (ev.ScriptId != environment.Script.Id)
                return;

            await _autoSaveScriptRepository.SaveAsync(environment.Script);
        });

        AddEnvironmentEventToken(environment, scriptConfigPropChangeToken);
    }

    private void ForwardRelevantEventsToIpc(ScriptEnvironment environment)
    {
        SubscribeAndForwardToIpc<EnvironmentPropertyChanged>(environment);
        SubscribeAndForwardToIpc<ScriptPropertyChanged>(environment, ev => ev.PropertyName != nameof(Script.Code));
        SubscribeAndForwardToIpc<ScriptConfigPropertyChanged>(environment);
    }

    private void SubscribeAndForwardToIpc<TEvent>(ScriptEnvironment environment, Func<TEvent, bool>? predicate = null) where TEvent : class, IEvent, IEventWithScriptId
    {
        var token = _eventBus.Subscribe<TEvent>(async ev =>
        {
            if (ev.ScriptId != environment.Script.Id)
            {
                return;
            }

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
}
