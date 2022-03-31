using NetPad.Scripts;

namespace NetPad.Events;

public class ScriptRanEvent : IEvent
{
    public ScriptRanEvent(ScriptEnvironment scriptEnvironment)
    {
        ScriptEnvironment = scriptEnvironment;
    }

    public ScriptEnvironment ScriptEnvironment { get; }
}
