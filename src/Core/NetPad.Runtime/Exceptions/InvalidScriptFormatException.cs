using NetPad.Scripts;

namespace NetPad.Exceptions;

public class InvalidScriptFormatException(string scriptName, string message) : Exception(message)
{
    public InvalidScriptFormatException(Script script, string message) : this(script.Name, message)
    {
        Script = script;
    }

    public string ScriptName { get; } = scriptName;
    public Script? Script { get; }
}
