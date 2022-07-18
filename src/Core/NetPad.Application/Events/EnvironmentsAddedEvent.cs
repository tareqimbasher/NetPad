using NetPad.Scripts;

namespace NetPad.Events;

public class EnvironmentsAddedEvent : IEvent
{
    public EnvironmentsAddedEvent(params ScriptEnvironment[] environments)
    {
        Environments = environments;
    }

    public ScriptEnvironment[] Environments { get; }
}
