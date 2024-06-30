using NetPad.Events;

namespace NetPad.Sessions.Events;

public class ActiveEnvironmentChangedEvent(Guid? scriptId) : IEvent
{
    public Guid? ScriptId { get; } = scriptId;
}
