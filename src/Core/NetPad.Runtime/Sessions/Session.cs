using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Data;
using NetPad.Events;
using NetPad.Exceptions;
using NetPad.Scripts;
using NetPad.Sessions.Events;

namespace NetPad.Sessions;

public class Session(
    IServiceScopeFactory serviceScopeFactory,
    ITrivialDataStore trivialDataStore,
    IEventBus eventBus,
    ILogger<Session> logger)
    : ISession
{
    private const string CurrentActiveSaveKey = "session.active";

    private readonly List<ScriptEnvironment> _environments = [];
    private Guid? _lastActiveScriptId;

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
            environment = new ScriptEnvironment(script, serviceScopeFactory.CreateScope());
            _environments.Add(environment);
            await eventBus.PublishAsync(new EnvironmentsAddedEvent(environment));
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

        if (Guid.TryParse(trivialDataStore.Get<string>(CurrentActiveSaveKey), out var lastSavedScriptId) &&
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
        logger.LogDebug("Closing script: {ScriptId}", scriptId);
        var environment = Get(scriptId);
        if (environment == null)
            return;

        var ix = _environments.IndexOf(environment);

        _environments.RemoveAt(ix);
        await eventBus.PublishAsync(new EnvironmentsRemovedEvent(environment));
        _ = Task.Run(async () => await environment.DisposeAsync());

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

        logger.LogDebug("Closed script: {ScriptId}", scriptId);
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
        eventBus.PublishAsync(new ActiveEnvironmentChangedEvent(newActive?.Script.Id));

        trivialDataStore.Set(CurrentActiveSaveKey, Active?.Script.Id);

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
