using NetPad.Scripts;

namespace NetPad.Events;

public class ScriptCodeUpdatedEvent : IEvent
{
    public ScriptCodeUpdatedEvent(Script script, string? newCode, string? oldCode)
    {
        Script = script;
        NewCode = newCode;
        OldCode = oldCode;
    }

    public Script Script { get; }
    public string? NewCode { get; }
    public string? OldCode { get; }
}
