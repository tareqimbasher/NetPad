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
                AutoSaveScriptChanges(environment);

                environment.SetIO(ActionInputReader.Null, new IpcScriptOutputWriter(environment, _ipcService));
            }

            return Task.CompletedTask;
        });

        _eventBus.Subscribe<EnvironmentsRemoved>(ev =>
        {
            Unsubscribe(ev.Environments);
            return Task.CompletedTask;
        });
    }

    private void AutoSaveScriptChanges(ScriptEnvironment environment)
    {
        async Task handler(Guid scriptId)
        {
            if (scriptId != environment.Script.Id)
                return;

            await _autoSaveScriptRepository.SaveAsync(environment.Script);
        }

        var scriptPropChangeToken = _eventBus.Subscribe<ScriptPropertyChanged>(async ev => await handler(ev.ScriptId));
        AddEnvironmentEventToken(environment, scriptPropChangeToken);

        var scriptConfigPropChangeToken = _eventBus.Subscribe<ScriptConfigPropertyChanged>(async ev => await handler(ev.ScriptId));
        AddEnvironmentEventToken(environment, scriptConfigPropChangeToken);
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
