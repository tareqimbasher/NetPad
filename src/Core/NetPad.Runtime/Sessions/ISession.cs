using NetPad.Scripts;

namespace NetPad.Sessions;

/// <summary>
/// Tracks which scripts are "open" and manages which script is active (currently focused).
/// </summary>
public interface ISession
{
    /// <summary>
    /// Currently open scripts.
    /// </summary>
    IReadOnlyList<ScriptEnvironment> Environments { get; }

    /// <summary>
    /// The currently active (focused) script.
    /// </summary>
    ScriptEnvironment? Active { get; }

    bool IsOpen(Guid scriptId);

    /// <summary>
    /// Attempts to get an open script by ID.
    /// </summary>
    /// <returns>The open script corresponding to the specified ID, or null if the script does not exist or is not open.</returns>
    ScriptEnvironment? Get(Guid scriptId);

    Task OpenAsync(Script script, bool activate);
    Task OpenAsync(IList<Script> scripts, bool activateBestCandidate);
    Task CloseAsync(Guid scriptId, bool activateNextScript);
    Task CloseAsync(IList<Guid> scriptIds, bool activateNextScript, bool persistSession);

    /// <summary>
    /// Makes the script corresponding to the specified ID active.
    /// </summary>
    Task ActivateAsync(Guid? scriptId);

    /// <summary>
    /// Activate the script that was active before the currently active one.
    /// </summary>
    Task ActivateLastActiveScriptAsync();

    /// <summary>
    /// Attempts to activate the most appropriate script within the session context when a specific target is not known.
    /// This method is intended for scenarios where the ideal script to activate cannot be explicitly determined,
    /// and a sensible default or best candidate should be chosen based on internal heuristics or context.
    /// </summary>
    Task ActivateBestCandidateAsync();
}
