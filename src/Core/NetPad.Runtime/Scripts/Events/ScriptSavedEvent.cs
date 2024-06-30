namespace NetPad.Scripts.Events;

public class ScriptSavedEvent(Script script) : IScriptEvent
{
    public Script Script { get; } = script;
    public Guid ScriptId => Script.Id;
}
