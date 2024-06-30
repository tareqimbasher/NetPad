namespace NetPad.Apps.CQs;

public class RequestNewScriptNameCommand(string currentScriptName) : Command<string?>
{
    public string CurrentScriptName { get; } = currentScriptName;
}
