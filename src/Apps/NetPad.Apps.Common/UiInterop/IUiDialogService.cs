using NetPad.Application;
using NetPad.Scripts;

namespace NetPad.Apps.UiInterop;

public interface IUiDialogService
{
    Task<YesNoCancel> AskUserIfTheyWantToSave(Script script);
    Task<string?> AskUserForSaveLocation(Script script);
    Task AlertUserAboutMissingDependencies(AppDependencyCheckResult dependencyCheckResult);
}
