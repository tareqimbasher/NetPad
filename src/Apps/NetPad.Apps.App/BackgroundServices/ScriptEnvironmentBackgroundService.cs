using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetPad.Events;
using NetPad.IO;
using NetPad.Scripts;
using NetPad.Services;
using NetPad.UiInterop;
using NetPad.Utilities;

namespace NetPad.BackgroundServices;

/// <summary>
/// Handles automations that occur when a script environment is added to removed from the session.
/// </summary>
public class ScriptEnvironmentBackgroundService : BackgroundService
{
    private readonly IEventBus _eventBus;
    private readonly IIpcService _ipcService;
    private readonly IAutoSaveScriptRepository _autoSaveScriptRepository;

    private readonly Dictionary<Guid, List<EventSubscriptionToken>> _environmentSubscriptionTokens;

    public ScriptEnvironmentBackgroundService(
        IEventBus eventBus,
        IIpcService ipcService,
        IAutoSaveScriptRepository autoSaveScriptRepository,
        ILoggerFactory loggerFactory) : base(loggerFactory)
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
        _eventBus.Subscribe<EnvironmentsAddedEvent>(ev =>
        {
            foreach (var environment in ev.Environments)
            {
                AutoSaveScriptChanges(environment);

                var outputAdapter = new ScriptOutputAdapter<ScriptOutput, ScriptOutput>(
                    new IpcScriptResultOutputWriter(environment.Script.Id, _ipcService),
                    new IpcScriptSqlOutputWriter(environment.Script.Id, _ipcService)
                );

                environment.SetIO(ActionInputReader.Null, outputAdapter);
            }

            return Task.CompletedTask;
        });

        _eventBus.Subscribe<EnvironmentsRemovedEvent>(ev =>
        {
            foreach (var environment in ev.Environments)
            {
                Unsubscribe(environment);
            }

            return Task.CompletedTask;
        });
    }

    private void AutoSaveScriptChanges(ScriptEnvironment environment)
    {
        var autoSave = new Func<Guid, Task>(async scriptId =>
        {
            if (scriptId != environment.Script.Id || !environment.Script.IsDirty)
                return;

            await _autoSaveScriptRepository.SaveAsync(environment.Script);
        }).DebounceAsync(3000);

        var scriptPropChangeToken = _eventBus.Subscribe<ScriptPropertyChangedEvent>(ev =>
        {
            autoSave(ev.ScriptId);
            return Task.CompletedTask;
        });
        AddEnvironmentEventToken(environment, scriptPropChangeToken);

        var scriptConfigPropChangeToken = _eventBus.Subscribe<ScriptConfigPropertyChangedEvent>(ev =>
        {
            autoSave(ev.ScriptId);
            return Task.CompletedTask;
        });
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
