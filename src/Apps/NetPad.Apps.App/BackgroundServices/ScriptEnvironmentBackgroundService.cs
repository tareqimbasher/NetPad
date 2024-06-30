using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using NetPad.Apps.CQs;
using NetPad.Apps.UiInterop;
using NetPad.Events;
using NetPad.IO;
using NetPad.Scripts;
using NetPad.Scripts.Events;
using NetPad.Services;
using NetPad.Sessions.Events;

namespace NetPad.BackgroundServices;

/// <summary>
/// Handles automations that occur when a script environment is added to removed from the session.
/// </summary>
public class ScriptEnvironmentBackgroundService(
    IEventBus eventBus,
    IIpcService ipcService,
    IAutoSaveScriptRepository autoSaveScriptRepository,
    ILoggerFactory loggerFactory)
    : BackgroundService(loggerFactory)
{
    private readonly ILoggerFactory _loggerFactory = loggerFactory;

    private readonly Dictionary<Guid, List<IDisposable>> _environmentSubscriptionTokens = new();

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ListenToEnvironmentsChanges();

        return Task.CompletedTask;
    }

    private void ListenToEnvironmentsChanges()
    {
        eventBus.Subscribe<EnvironmentsAddedEvent>(ev =>
        {
            foreach (var environment in ev.Environments)
            {
                AutoSaveScriptChanges(environment);

                var inputReader = new AsyncActionInputReader<string>(
                    // TODO There should be a way to cancel the wait when the environment stops using a CancellationToken
                    async () => await ipcService.SendAndReceiveAsync(new PromptUserForInputCommand(environment.Script.Id)));

                var outputWriter = new ScriptEnvironmentIpcOutputWriter(
                    environment,
                    ipcService,
                    eventBus,
                    _loggerFactory.CreateLogger<ScriptEnvironmentIpcOutputWriter>());

                environment.SetIO(inputReader, outputWriter);

                AddEnvironmentEventToken(environment, new DisposableToken(() => outputWriter.Dispose()));
            }

            return Task.CompletedTask;
        });

        eventBus.Subscribe<EnvironmentsRemovedEvent>(ev =>
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

            await autoSaveScriptRepository.SaveAsync(environment.Script);
        }).DebounceAsync(3000);

        var scriptPropChangeToken = eventBus.Subscribe<ScriptPropertyChangedEvent>(ev =>
        {
            autoSave(ev.ScriptId);
            return Task.CompletedTask;
        });
        AddEnvironmentEventToken(environment, scriptPropChangeToken);

        var scriptConfigPropChangeToken = eventBus.Subscribe<ScriptConfigPropertyChangedEvent>(ev =>
        {
            autoSave(ev.ScriptId);
            return Task.CompletedTask;
        });
        AddEnvironmentEventToken(environment, scriptConfigPropChangeToken);
    }

    private void AddEnvironmentEventToken(ScriptEnvironment environment, IDisposable token)
    {
        if (!_environmentSubscriptionTokens.TryGetValue(environment.Script.Id, out var tokens))
        {
            tokens = [];
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
            token.Dispose();
        }

        _environmentSubscriptionTokens.Remove(environment.Script.Id);
    }
}
