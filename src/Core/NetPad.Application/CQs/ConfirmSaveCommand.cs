using NetPad.Scripts;
using NetPad.UiInterop;

namespace NetPad.CQs;

public class ConfirmSaveCommand : Command<YesNoCancel>
{
    public ConfirmSaveCommand(Script script)
    {
        Message = $"You have unsaved changes. Do you want to save '{script.Name}'?";
    }

    public string Message { get; }
}
