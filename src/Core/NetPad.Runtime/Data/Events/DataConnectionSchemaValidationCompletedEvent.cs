using NetPad.Events;

namespace NetPad.Data.Events;

public class DataConnectionSchemaValidationCompletedEvent(Guid dataConnectionId) : IEvent
{
    public Guid DataConnectionId { get; } = dataConnectionId;
}
