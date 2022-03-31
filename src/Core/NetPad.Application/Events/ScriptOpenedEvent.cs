using NetPad.Scripts;

namespace NetPad.Events;

public class ScriptOpenedEvent : IEvent
{
    public ScriptOpenedEvent(Script script)
    {
        Script = script;
    }

    public Script Script { get; }
}
