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
    private readonly OmniSharpServerCatalog _omniSharpServerCatalog;

    private readonly Dictionary<Guid, List<EventSubscriptionToken>> _environmentSubscriptionTokens;

    public ScriptEnvironmentBackgroundService(
        IEventBus eventBus,
        IIpcService ipcService,
        IAutoSaveScriptRepository autoSaveScriptRepository,
        OmniSharpServerCatalog omniSharpServerCatalog)
    {
        _eventBus = eventBus;
        _ipcService = ipcService;
        _autoSaveScriptRepository = autoSaveScriptRepository;
        _omniSharpServerCatalog = omniSharpServerCatalog;
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

                // We don't want to wait for this, we want it to occur asynchronously
#pragma warning disable CS4014
                _omniSharpServerCatalog.StartOmniSharpServerAsync(environment);
#pragma warning restore CS4014

                environment.SetIO(ActionInputReader.Null, new IpcScriptOutputWriter(environment, _ipcService));
            }

            return Task.CompletedTask;
        });

        _eventBus.Subscribe<EnvironmentsRemoved>(async ev =>
        {
            foreach (var environment in ev.Environments)
            {
                Unsubscribe(environment);
                await _omniSharpServerCatalog.StopOmniSharpServerAsync(environment);
            }
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

    private void Unsubscribe(ScriptEnvironment environment)
    {
        if (!_environmentSubscriptionTokens.TryGetValue(environment.Script.Id, out var tokens))
        {
            return;
        }

        foreach (var token in tokens)
        {
            _eventBus.Unsubscribe(token);
        }

        _environmentSubscriptionTokens.Remove(environment.Script.Id);
    }
}
