using NetPad.Scripts;

namespace NetPad.Sessions;

public interface ISession
{
    IReadOnlyList<ScriptEnvironment> Environments { get; }
    ScriptEnvironment? Active { get; }

    bool IsOpen(Guid scriptId);
    ScriptEnvironment? Get(Guid scriptId);
    Task OpenAsync(Script script, bool activate);
    Task OpenAsync(IList<Script> scripts, bool activateBestCandidate);
    Task CloseAsync(Guid scriptId, bool activateNextScript);
    Task CloseAsync(IList<Guid> scriptIds, bool activateNextScript, bool persistSession);
    Task ActivateAsync(Guid? scriptId);
    Task ActivateLastActiveScriptAsync();
    Task ActivateBestCandidateAsync();
}
