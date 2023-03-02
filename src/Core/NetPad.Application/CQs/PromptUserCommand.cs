namespace NetPad.CQs;

public class PromptUserCommand : Command<string?>
{
    public PromptUserCommand(string message, string? prefillValue)
    {
        Message = message;
        PrefillValue = prefillValue;
    }

    public string Message { get; }
    public string? PrefillValue { get; }
}
