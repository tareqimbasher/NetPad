using NetPad.Apps.UiInterop;
using NetPad.Scripts;

namespace NetPad.Apps.CQs;

public class ConfirmSaveCommand(Script script) : Command<YesNoCancel>
{
    public string Message { get; } = $"'{script.Name}' has unsaved changes. Do you want to save?";
}
