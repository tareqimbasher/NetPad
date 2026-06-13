using NetPad.Application;
using NetPad.Scripts;

namespace NetPad.Apps.UiInterop;

/// <summary>
/// Provides high level methods that this application backend can use to show dialogs on the UI.
/// </summary>
public interface IUiDialogService
{
    Task<YesNoCancel> AskUserIfTheyWantToSave(Script script);
    Task<string?> AskUserForSaveLocation(Script script);

    /// <summary>
    /// Asks the user whether they want to open <paramref name="newPath"/> as a separate script with a fresh ID
    /// because another script with the same ID is already open at <paramref name="existingPath"/>.
    /// Returns <c>true</c> to open it as a separate script, <c>false</c> to activate the existing tab instead.
    /// </summary>
    /// <param name="newPath">The path the user is trying to open. This file will be assigned a new ID on first save.</param>
    /// <param name="existingPath">The path of the script that is already open and shares the same ID.</param>
    Task<bool> AskUserToOpenAsDuplicate(string newPath, string existingPath);

    Task AlertUserAboutMissingDependencies(AppDependencyCheckResult dependencyCheckResult);
}
