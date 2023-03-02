using NetPad.UiInterop;

namespace NetPad.CQs;

public class ConfirmWithUserCommand : Command<YesNoCancel>
{
    public ConfirmWithUserCommand(string message)
    {
        Message = message;
    }

    public string Message { get; }
}
