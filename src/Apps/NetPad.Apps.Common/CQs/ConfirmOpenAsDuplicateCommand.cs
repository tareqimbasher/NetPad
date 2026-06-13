namespace NetPad.Apps.CQs;

/// <summary>
/// Asks the user (via IPC) whether they want to open a script as a separate copy because another script
/// with the same ID is already open at a different path. The frontend dialog handler should resolve
/// with <c>true</c> to open as a separate script, or <c>false</c> to activate the existing tab.
/// </summary>
public class ConfirmOpenAsDuplicateCommand(string newPath, string existingPath) : Command<bool>
{
    public string NewPath { get; } = newPath;
    public string ExistingPath { get; } = existingPath;

    public string Message { get; } =
        $"A script with the same ID is already open from '{existingPath}'. " +
        $"The file you're opening ('{newPath}') will be assigned a new ID on its first save. Continue?";
}
