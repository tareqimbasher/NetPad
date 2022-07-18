using NetPad.Scripts;

namespace NetPad.Events;

public class EnvironmentsRemovedEvent : IEvent
{
    public EnvironmentsRemovedEvent(params ScriptEnvironment[] environments)
    {
        Environments = environments;
    }

    public ScriptEnvironment[] Environments { get; }
}
