namespace NetPad.Apps.CQs;

public class PromptUserCommand(string message, string? prefillValue) : Command<string?>
{
    public string Message { get; } = message;
    public string? PrefillValue { get; } = prefillValue;
}
