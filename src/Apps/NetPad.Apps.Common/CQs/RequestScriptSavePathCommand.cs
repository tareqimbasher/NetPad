namespace NetPad.Apps.CQs;

public class RequestScriptSavePathCommand(string scriptName, string? defaultPath = null) : Command<string?>
{
    public string ScriptName { get; } = scriptName;
    public string? DefaultPath { get; } = defaultPath;
}
