namespace NetPad.ExecutionModel.ClientServer.ScriptServices;

/// <summary>
/// Information about the current script.
/// </summary>
/// <param name="Id">Script ID.</param>
/// <param name="Name">Script name.</param>
/// <param name="FilePath">The full path of the script file. Returns null if script isn't saved.</param>
/// <param name="IsDirty">Whether the script has unsaved changes or not.</param>
public record UserScript(
    Guid Id,
    string Name,
    string? FilePath,
    bool IsDirty);
