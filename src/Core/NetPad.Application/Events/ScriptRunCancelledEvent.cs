using NetPad.Scripts;

namespace NetPad.Events;

public class ScriptRunCancelledEvent : IEvent
{
    public ScriptRunCancelledEvent(ScriptEnvironment scriptEnvironment)
    {
        ScriptEnvironment = scriptEnvironment;
    }

    public ScriptEnvironment ScriptEnvironment { get; }
}
