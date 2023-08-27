namespace NetPad.Events;

public class DataConnectionSchemaValidationCompletedEvent : IEvent
{
    public DataConnectionSchemaValidationCompletedEvent(Guid dataConnectionId)
    {
        DataConnectionId = dataConnectionId;
    }

    public Guid DataConnectionId { get; }
}
