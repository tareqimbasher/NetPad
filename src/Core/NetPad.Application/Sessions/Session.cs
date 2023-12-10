using Microsoft.Extensions.Logging;
using NetPad.Data;
using NetPad.Events;
using NetPad.Exceptions;
using NetPad.Scripts;

namespace NetPad.Sessions;

public class Session : ISession
{
    private const string CurrentActiveSaveKey = "session.active";

    private readonly IScriptEnvironmentFactory _scriptEnvironmentFactory;
    private readonly ITrivialDataStore _trivialDataStore;
    private readonly IEventBus _eventBus;
    private readonly ILogger<Session> _logger;
    private readonly List<ScriptEnvironment> _environments;
    private Guid? _lastActiveScriptId;

    public Session(
        IScriptEnvironmentFactory scriptEnvironmentFactory,
        ITrivialDataStore trivialDataStore,
        IEventBus eventBus,
        ILogger<Session> logger)
    {
        _scriptEnvironmentFactory = scriptEnvironmentFactory;
        _trivialDataStore = trivialDataStore;
        _eventBus = eventBus;
        _logger = logger;
        _environments = new List<ScriptEnvironment>();
    }

    public IReadOnlyList<ScriptEnvironment> Environments => _environments.AsReadOnly();

    public ScriptEnvironment? Active { get; private set; }


    public ScriptEnvironment? Get(Guid scriptId)
    {
        return _environments.FirstOrDefault(m => m.Script.Id == scriptId);
    }

    public async Task OpenAsync(Script script, bool activate = true)
    {
        var environment = Get(script.Id);

        if (environment == null)
        {
            environment = await _scriptEnvironmentFactory.CreateEnvironmentAsync(script);
            _environments.Add(environment);
            await _eventBus.PublishAsync(new EnvironmentsAddedEvent(environment));
        }

        if (activate)
        {
            await ActivateAsync(script.Id);
        }
    }

    public async Task OpenAsync(IEnumerable<Script> scripts)
    {
        scripts = scripts.ToArray();

        foreach (var script in scripts)
        {
            await OpenAsync(script, activate: false);
        }

        if (Guid.TryParse(_trivialDataStore.Get<string>(CurrentActiveSaveKey), out var lastSavedScriptId) &&
            scripts.Any(s => s.Id == lastSavedScriptId))
        {
            await ActivateAsync(lastSavedScriptId);
        }
        else
        {
            await ActivateAsync(scripts.LastOrDefault()?.Id);
        }
    }

    public async Task CloseAsync(Guid scriptId, bool activateNextScript = true)
    {
        _logger.LogDebug("Closing script: {ScriptId}", scriptId);
        var environment = Get(scriptId);
        if (environment == null)
            return;

        var ix = _environments.IndexOf(environment);

        _environments.RemoveAt(ix);
        await environment.DisposeAsync();
        await _eventBus.PublishAsync(new EnvironmentsRemovedEvent(environment));

        if (activateNextScript && Active == environment)
        {
            if (_environments.Any())
            {
                if (CanActivateLastActiveScript())
                    await ActivateAsync(_lastActiveScriptId);
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

        _logger.LogDebug("Closed script: {ScriptId}", scriptId);
    }

    public async Task CloseAsync(IEnumerable<Guid> scriptIds)
    {
        var ids = scriptIds.ToArray();

        for (int i = 0; i < ids.Length; i++)
        {
            var id = ids[i];

            bool isLastScriptInBatch = i == ids.Length - 1;

            await CloseAsync(id, isLastScriptInBatch);
        }
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

        _trivialDataStore.Set(CurrentActiveSaveKey, Active?.Script.Id);

        return Task.CompletedTask;
    }

    public Task ActivateLastActiveScriptAsync()
    {
        if (CanActivateLastActiveScript())
            ActivateAsync(_lastActiveScriptId);

        return Task.CompletedTask;
    }

    private bool CanActivateLastActiveScript() =>
        _lastActiveScriptId != null && _environments.Any(e => e.Script.Id == _lastActiveScriptId);
}
