using NetPad.Data;

namespace NetPad.Events;

public class DataConnectionSavedEvent : IEvent
{
    public DataConnectionSavedEvent(DataConnection dataConnection)
    {
        DataConnection = dataConnection;
    }

    public DataConnection DataConnection { get; }
}
