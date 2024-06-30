using NetPad.Events;

namespace NetPad.Scripts.Events;

public class ScriptRanEvent(ScriptEnvironment scriptEnvironment) : IEvent
{
    public ScriptEnvironment ScriptEnvironment { get; } = scriptEnvironment;
}
