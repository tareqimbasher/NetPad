using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Data;
using NetPad.Events;
using NetPad.Exceptions;
using NetPad.Scripts;
using NetPad.Sessions.Events;

namespace NetPad.Sessions;

public class Session : ISession
{
    private const string ActiveSaveKey = "session.active";
    public const string OpenScriptsSaveKey = "session.openScripts";

    private readonly List<ScriptEnvironment> _environments;
    private readonly Action _saveSessionInfoDebounced;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ITrivialDataStore _trivialDataStore;
    private readonly IEventBus _eventBus;
    private readonly ILogger<Session> _logger;
    private Guid? _lastActiveScriptId;

    public Session(
        IServiceScopeFactory serviceScopeFactory,
        ITrivialDataStore trivialDataStore,
        IEventBus eventBus,
        ILogger<Session> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _trivialDataStore = trivialDataStore;
        _eventBus = eventBus;
        _logger = logger;
        _environments = [];
        Environments = _environments.AsReadOnly();

        _saveSessionInfoDebounced = DelegateUtil.Debounce(SaveSessionInfo, 500);
    }

    public IReadOnlyList<ScriptEnvironment> Environments { get; }

    public ScriptEnvironment? Active { get; private set; }

    public bool IsOpen(Guid scriptId) => Environments.Any(e => e.Script.Id == scriptId);

    public ScriptEnvironment? Get(Guid scriptId)
    {
        return _environments.FirstOrDefault(m => m.Script.Id == scriptId);
    }

    public async Task OpenAsync(Script script, bool activate)
    {
        await OpenInternalAsync(script);

        if (activate)
        {
            await ActivateAsync(script.Id);
        }

        _saveSessionInfoDebounced();
    }

    public async Task OpenAsync(IList<Script> scripts, bool activateBestCandidate)
    {
        if (scripts.Count == 0)
        {
            return;
        }

        scripts = scripts.ToArray();

        foreach (var script in scripts)
        {
            await OpenInternalAsync(script);
        }

        if (activateBestCandidate)
        {
            await ActivateBestCandidateAsync();
        }

        _saveSessionInfoDebounced();
    }

    private async Task OpenInternalAsync(Script script)
    {
        var environment = Get(script.Id);

        if (environment == null)
        {
            environment = new ScriptEnvironment(script, _serviceScopeFactory.CreateScope());
            _environments.Add(environment);
            await _eventBus.PublishAsync(new EnvironmentsAddedEvent(environment));
        }
    }

    public async Task CloseAsync(Guid scriptId, bool activateNextScript)
    {
        var environment = await CloseInternalAsync(scriptId, activateNextScript);

        if (_environments.Count == 0)
        {
            await ActivateAsync(null);
        }

        if (environment != null)
        {
            _saveSessionInfoDebounced();
        }
    }

    public async Task CloseAsync(IList<Guid> scriptIds, bool activateNextScript, bool persistSession)
    {
        for (int i = 0; i < scriptIds.Count; i++)
        {
            var id = scriptIds[i];

            bool isLastScriptInBatch = i == scriptIds.Count - 1;

            await CloseInternalAsync(id, activateNextScript && isLastScriptInBatch);
        }

        if (persistSession)
        {
            _saveSessionInfoDebounced();
        }
    }

    private async Task<ScriptEnvironment?> CloseInternalAsync(Guid scriptId, bool activateNextScript)
    {
        var environment = Get(scriptId);
        if (environment == null)
        {
            return null;
        }

        _logger.LogDebug("Closing script: {ScriptId}", scriptId);

        var ix = _environments.IndexOf(environment);
        _environments.RemoveAt(ix);
        await _eventBus.PublishAsync(new EnvironmentsRemovedEvent(environment));
        _ = Task.Run(async () => await environment.DisposeAsync());

        _logger.LogDebug("Closed script: {ScriptId}", scriptId);

        if (activateNextScript && Active == environment)
        {
            if (_environments.Count > 0)
            {
                if (CanActivateLastActiveScript())
                {
                    await ActivateAsync(_lastActiveScriptId);
                }
                else
                {
                    ix = ix == 0 ? 1 : ix - 1;
                    await ActivateAsync(_environments[ix].Script.Id);
                }
            }
            else
            {
                await ActivateAsync(null);
            }
        }

        return environment;
    }

    public Task ActivateAsync(Guid? scriptId)
    {
        ScriptEnvironment? newActive;
        _lastActiveScriptId = Active?.Script.Id;

        if (scriptId == Active?.Script.Id)
        {
            return Task.CompletedTask;
        }

        if (scriptId == null)
        {
            newActive = null;
        }
        else
        {
            var environment = Get(scriptId.Value);
            newActive = environment ?? throw new EnvironmentNotFoundException(scriptId.Value);
        }

        Active = newActive;
        _eventBus.PublishAsync(new ActiveEnvironmentChangedEvent(newActive?.Script.Id));

        _trivialDataStore.Set(ActiveSaveKey, Active?.Script.Id);

        return Task.CompletedTask;
    }

    public Task ActivateLastActiveScriptAsync()
    {
        if (CanActivateLastActiveScript())
            ActivateAsync(_lastActiveScriptId);

        return Task.CompletedTask;
    }

    public async Task ActivateBestCandidateAsync()
    {
        // If a script is already active, nothing more to do
        if (Active != null || _environments.Count == 0)
        {
            return;
        }

        await ActivateLastActiveScriptAsync();

        if (Active != null)
        {
            return;
        }

        // Try to activate the last activated script from data store
        var lastActiveScriptIdStr = _trivialDataStore.Get<string>(ActiveSaveKey);
        if (lastActiveScriptIdStr != null
            && Guid.TryParse(lastActiveScriptIdStr, out var lastActiveScriptId)
            && IsOpen(lastActiveScriptId))
        {
            await ActivateAsync(lastActiveScriptId);
        }

        // If we still haven't activated a script, activate the last one
        if (Active == null)
        {
            await ActivateAsync(Environments[^1].Script.Id);
        }
    }

    private bool CanActivateLastActiveScript() =>
        _lastActiveScriptId != null && _environments.Any(e => e.Script.Id == _lastActiveScriptId);


    private void SaveSessionInfo()
    {
        try
        {
            var scriptIds = _environments.Select(x => x.Script.Id).ToArray();
            if (scriptIds.Length == 0)
            {
                return;
            }

            _trivialDataStore.Set(OpenScriptsSaveKey, scriptIds);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error saving session info");
        }
    }
}
