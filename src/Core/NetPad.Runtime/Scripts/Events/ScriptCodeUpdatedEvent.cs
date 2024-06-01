using NetPad.Events;

namespace NetPad.Scripts.Events;

public class ScriptCodeUpdatedEvent(Script script, string? newCode, string? oldCode) : IEvent
{
    public Script Script { get; } = script;
    public string? NewCode { get; } = newCode;
    public string? OldCode { get; } = oldCode;
}
