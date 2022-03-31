using NetPad.Scripts;

namespace NetPad.Events;

public class ScriptSavedEvent : IScriptEvent
{
    public ScriptSavedEvent(Script script)
    {
        Script = script;
    }

    public Script Script { get; }
    public Guid ScriptId => Script.Id;
}
