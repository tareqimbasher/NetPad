namespace NetPad.Commands;

public class RequestNewScriptNameCommand : Command<string?>
{
    public RequestNewScriptNameCommand(string currentScriptName)
    {
        CurrentScriptName = currentScriptName;
    }

    public string CurrentScriptName { get; }
}
