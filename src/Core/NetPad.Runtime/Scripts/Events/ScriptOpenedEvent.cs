using NetPad.Events;

namespace NetPad.Scripts.Events;

public class ScriptOpenedEvent(Script script) : IEvent
{
    public Script Script { get; } = script;
}
