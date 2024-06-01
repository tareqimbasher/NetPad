using NetPad.Events;
using NetPad.Scripts;

namespace NetPad.Sessions.Events;

public class EnvironmentsAddedEvent(params ScriptEnvironment[] environments) : IEvent
{
    public ScriptEnvironment[] Environments { get; } = environments;
}
