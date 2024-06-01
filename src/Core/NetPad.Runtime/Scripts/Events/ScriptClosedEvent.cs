using NetPad.Events;

namespace NetPad.Scripts.Events;

public class ScriptClosedEvent(Script script) : IEvent
{
    public Script Script { get; } = script;
}
