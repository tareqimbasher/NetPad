using NetPad.Apps.UiInterop;

namespace NetPad.Apps.CQs;

public class ConfirmWithUserCommand(string message) : Command<YesNoCancel>
{
    public string Message { get; } = message;
}
