using NetPad.Data;

namespace NetPad.Events;

public class DataConnectionDeletedEvent : IEvent
{
    public DataConnectionDeletedEvent(DataConnection dataConnection)
    {
        DataConnection = dataConnection;
    }

    public DataConnection DataConnection { get; }
}
