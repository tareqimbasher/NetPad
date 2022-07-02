using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NetPad.Configuration;
using NetPad.Events;
using NetPad.IO;
using NetPad.Scripts;
using NetPad.Services;
using NetPad.Services.OmniSharp;
using NetPad.Sessions;
using NetPad.UiInterop;

namespace NetPad.BackgroundServices;

/// <summary>
/// Handles automations that occur when a script environment is added to removed from the session.
/// </summary>
public class ScriptEnvironmentBackgroundService : BackgroundService
{
    private readonly ISession _session;
    private readonly IEventBus _eventBus;
    private readonly IIpcService _ipcService;
    private readonly IAutoSaveScriptRepository _autoSaveScriptRepository;
    private readonly OmniSharpServerCatalog _omniSharpServerCatalog;
    private readonly Settings _settings;

    private readonly Dictionary<Guid, List<EventSubscriptionToken>> _environmentSubscriptionTokens;

    public ScriptEnvironmentBackgroundService(
        ISession session,
        IEventBus eventBus,
        IIpcService ipcService,
        IAutoSaveScriptRepository autoSaveScriptRepository,
        OmniSharpServerCatalog omniSharpServerCatalog,
        Settings settings,
        ILoggerFactory loggerFactory) : base(loggerFactory)
    {
        _session = session;
        _eventBus = eventBus;
        _ipcService = ipcService;
        _autoSaveScriptRepository = autoSaveScriptRepository;
        _omniSharpServerCatalog = omniSharpServerCatalog;
        _settings = settings;
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
            foreach (var environment in ev.Environments)
            {
                Unsubscribe(environment);

                // We don't want to wait for this
#pragma warning disable CS4014
                _omniSharpServerCatalog.StopOmniSharpServerAsync(environment);
#pragma warning restore CS4014
            }

            return Task.CompletedTask;
        });

        _eventBus.Subscribe<ActiveEnvironmentChanged>(ev =>
        {
            var activatedEnvironmentScriptId = ev.ScriptId;

            if (activatedEnvironmentScriptId == null || !_settings.EditorOptions.CodeCompletion.Enabled)
            {
                return Task.CompletedTask;
            }

            bool hasActiveOmniSharpServer = _omniSharpServerCatalog.GetOmniSharpServer(activatedEnvironmentScriptId.Value) != null;
            if (hasActiveOmniSharpServer)
            {
                return Task.CompletedTask;
            }

            var environment = _session.Get(activatedEnvironmentScriptId.Value);
            if (environment != null)
            {
                try
                {
                    // We don't want to wait for this, we want it to occur asynchronously
#pragma warning disable CS4014
                    _omniSharpServerCatalog.StartOmniSharpServerAsync(environment);
#pragma warning restore CS4014
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred starting OmniSharp server for script {Script}", environment.Script);
                }
            }

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
