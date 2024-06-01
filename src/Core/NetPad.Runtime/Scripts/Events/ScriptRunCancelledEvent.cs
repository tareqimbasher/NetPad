using NetPad.Events;

namespace NetPad.Scripts.Events;

public class ScriptRunCancelledEvent(ScriptEnvironment scriptEnvironment) : IEvent
{
    public ScriptEnvironment ScriptEnvironment { get; } = scriptEnvironment;
}
