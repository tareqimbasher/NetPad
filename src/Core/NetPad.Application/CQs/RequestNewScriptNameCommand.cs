namespace NetPad.CQs;

public class RequestNewScriptNameCommand : Command<string?>
{
    public RequestNewScriptNameCommand(string currentScriptName)
    {
        CurrentScriptName = currentScriptName;
    }

    public string CurrentScriptName { get; }
}
