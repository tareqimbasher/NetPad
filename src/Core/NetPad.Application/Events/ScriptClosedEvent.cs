using NetPad.Scripts;

namespace NetPad.Events;

public class ScriptClosedEvent : IEvent
{
    public ScriptClosedEvent(Script script)
    {
        Script = script;
    }

    public Script Script { get; }
}
