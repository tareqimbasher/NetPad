namespace NetPad.CQs;

public class PromptUserForInputCommand : Command<string?>
{
    public PromptUserForInputCommand(Guid scriptId)
    {
        ScriptId = scriptId;
    }

    public Guid ScriptId { get; }
}
