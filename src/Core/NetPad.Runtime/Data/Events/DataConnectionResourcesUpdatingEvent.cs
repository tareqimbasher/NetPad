using NetPad.Events;

namespace NetPad.Data.Events;

public class DataConnectionResourcesUpdatingEvent(
    DataConnection dataConnection,
    DataConnectionResourceComponent updatingComponent)
    : IEvent
{
    public DataConnection DataConnection { get; } = dataConnection;
    public DataConnectionResourceComponent UpdatingComponent { get; } = updatingComponent;
}
