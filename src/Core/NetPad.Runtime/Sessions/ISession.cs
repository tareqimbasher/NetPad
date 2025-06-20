using NetPad.Scripts;

namespace NetPad.Sessions;

/// <summary>
/// Tracks which scripts are "open" and manages which script is active (currently focused).
/// </summary>
public interface ISession
{
    /// <summary>
    /// Returns the list of currently open script environments.
    /// </summary>
    List<ScriptEnvironment> GetOpened();

    /// <summary>
    /// Gets the currently active (focused) script environment, if any.
    /// </summary>
    ScriptEnvironment? Active { get; }

    /// <summary>
    /// Determines whether a script with the specified ID is currently open.
    /// </summary>
    /// <param name="scriptId">The unique identifier of the script to check.</param>
    /// <returns><c>true</c> if the script is open; otherwise, <c>false</c>.</returns>
    bool IsOpen(Guid scriptId);

    /// <summary>
    /// Retrieves an open script environment by its identifier.
    /// </summary>
    /// <param name="scriptId">The unique identifier of the script to retrieve.</param>
    /// <returns>
    /// The <see cref="ScriptEnvironment"/> for the specified <paramref name="scriptId"/>,
    /// or <c>null</c> if the script is not open or does not exist.
    /// </returns>
    ScriptEnvironment? Get(Guid scriptId);

    /// <summary>
    /// Opens the specified script in a new script environment.
    /// </summary>
    /// <param name="script">The script to open.</param>
    /// <param name="activate">
    /// If <c>true</c>, sets the newly opened script as the active script.
    /// </param>
    Task OpenAsync(Script script, bool activate);

    /// <summary>
    /// Opens multiple scripts in new script environments.
    /// </summary>
    /// <param name="scripts">The collection of scripts to open.</param>
    /// <param name="activateBestCandidate">
    /// If <c>true</c>, activates the best candidate among those opened
    /// based on internal heuristics; otherwise, does not change the active script.
    /// </param>
    Task OpenAsync(IList<Script> scripts, bool activateBestCandidate);

    /// <summary>
    /// Closes the script environment matching the specified ID.
    /// </summary>
    /// <param name="scriptId">The unique identifier of the script to close.</param>
    Task CloseAsync(Guid scriptId);

    /// <summary>
    /// Closes multiple script environments.
    /// </summary>
    /// <param name="scriptIds">A list of script IDs to close.</param>
    /// <param name="activateNextScript">
    /// If <c>true</c>, activates the next script after closing the last one in <paramref name="scriptIds"/>.
    /// </param>
    /// <param name="persistSession">
    /// If <c>true</c>, persists the session state after closing all specified scripts.
    /// </param>
    Task CloseAsync(IList<Guid> scriptIds, bool activateNextScript, bool persistSession);

    /// <summary>
    /// Sets the specified script as the active (focused) environment.
    /// </summary>
    /// <param name="scriptId">
    /// The unique identifier of the script to activate,
    /// or <c>null</c> to clear the current active script.
    /// </param>
    Task ActivateAsync(Guid? scriptId);

    /// <summary>
    /// Reactivates the last active script environment, if one exists.
    /// </summary>
    Task ActivateLastActiveScriptAsync();

    /// <summary>
    /// Attempts to activate the most appropriate script within the session context when a specific target is not known.
    /// This method is intended for scenarios where the ideal script to activate cannot be explicitly determined,
    /// and a sensible default or best candidate should be chosen based on internal heuristics or context.
    /// </summary>
    Task ActivateBestCandidateAsync();
}
