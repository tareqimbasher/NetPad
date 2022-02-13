using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetPad.Events;
using NetPad.Runtimes;
using NetPad.Scripts;
using NetPad.Services;
using NetPad.UiInterop;

namespace NetPad.BackgroundServices;

public class ScriptEnvironmentBackgroundService : BackgroundService
{
    private readonly IEventBus _eventBus;
    private readonly IIpcService _ipcService;

    private readonly Dictionary<Guid, List<EventSubscriptionToken>> _environmentSubscriptionTokens;

    public ScriptEnvironmentBackgroundService(IEventBus eventBus, IIpcService ipcService)
    {
        _eventBus = eventBus;
        _ipcService = ipcService;
        _environmentSubscriptionTokens = new Dictionary<Guid, List<EventSubscriptionToken>>();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        PushEnvironmentsChanges();

        return Task.CompletedTask;
    }

    private void PushEnvironmentsChanges()
    {
        _eventBus.Subscribe<EnvironmentsAdded>(ev =>
        {
            foreach (var environment in ev.Environments)
            {
                SubscribeAndForwardToIpc<EnvironmentPropertyChanged>(environment);
                SubscribeAndForwardToIpc<ScriptPropertyChanged>(environment);
                SubscribeAndForwardToIpc<ScriptConfigPropertyChanged>(environment);

                environment.SetIO(ActionRuntimeInputReader.Null, new IpcScriptOutputWriter(environment, _ipcService));
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

    private void SubscribeAndForwardToIpc<TEvent>(ScriptEnvironment environment) where TEvent : class, IEvent
    {
        var token = _eventBus.Subscribe<TEvent>(async ev =>
        {
            await _ipcService.SendAsync(ev);
        });

        if (!_environmentSubscriptionTokens.TryGetValue(environment.Script.Id, out var tokens))
        {
            tokens = new List<EventSubscriptionToken>();
            _environmentSubscriptionTokens.Add(environment.Script.Id, tokens);
        }

        tokens.Add(token);
    }
}
