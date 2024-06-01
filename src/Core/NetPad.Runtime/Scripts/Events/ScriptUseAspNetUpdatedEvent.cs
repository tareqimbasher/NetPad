using NetPad.Events;

namespace NetPad.Scripts.Events;

public class ScriptUseAspNetUpdatedEvent(Script script, bool oldValue, bool newValue) : IEvent
{
    public Script Script { get; } = script;
    public bool OldValue { get; } = oldValue;
    public bool NewValue { get; } = newValue;
}
