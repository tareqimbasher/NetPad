using NetPad.Events;

namespace NetPad.Scripts.Events;

public class ScriptKindUpdatedEvent(Script script, ScriptKind oldValue, ScriptKind newValue)
    : IEvent
{
    public Script Script { get; } = script;
    public ScriptKind OldValue { get; } = oldValue;
    public ScriptKind NewValue { get; } = newValue;
}
