namespace NetPad.Apps.CQs;

public class PromptUserForInputCommand(Guid scriptId) : Command<string?>
{
    public Guid ScriptId { get; } = scriptId;
}
