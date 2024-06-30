namespace NetPad.Apps.CQs;

public class AlertUserCommand(string message) : Command
{
    public string Message { get; } = message;
}
