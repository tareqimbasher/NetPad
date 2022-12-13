using NetPad.Data;

namespace NetPad.Events;

public class DataConnectionResourcesUpdatingEvent : IEvent
{
    public DataConnectionResourcesUpdatingEvent(DataConnection dataConnection, DataConnectionResourceComponent updatingComponent)
    {
        DataConnection = dataConnection;
        UpdatingComponent = updatingComponent;
    }

    public DataConnection DataConnection { get; }
    public DataConnectionResourceComponent UpdatingComponent { get; }
}
