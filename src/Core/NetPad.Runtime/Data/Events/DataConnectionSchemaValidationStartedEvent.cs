using NetPad.Events;

namespace NetPad.Data.Events;

public class DataConnectionSchemaValidationStartedEvent(Guid dataConnectionId) : IEvent
{
    public Guid DataConnectionId { get; } = dataConnectionId;
}
