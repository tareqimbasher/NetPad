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
    Task AlertUserAboutMissingDependencies(AppDependencyCheckResult dependencyCheckResult);
}
