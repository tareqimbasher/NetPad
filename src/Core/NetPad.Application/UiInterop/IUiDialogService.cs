using NetPad.Scripts;

namespace NetPad.UiInterop;

public interface IUiDialogService
{
    Task<YesNoCancel> AskUserIfTheyWantToSave(Script script);
    Task<string?> AskUserForSaveLocation(Script script);
}
