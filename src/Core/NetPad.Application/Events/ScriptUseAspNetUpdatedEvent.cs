using NetPad.Scripts;

namespace NetPad.Events;

public class ScriptUseAspNetUpdatedEvent : IEvent
{
    public ScriptUseAspNetUpdatedEvent(Script script, bool oldValue, bool newValue)
    {
        Script = script;
        OldValue = oldValue;
        NewValue = newValue;
    }

    public Script Script { get; }
    public bool OldValue { get; }
    public bool NewValue { get; }
}
