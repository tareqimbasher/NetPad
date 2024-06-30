using NetPad.Events;

namespace NetPad.Data.Events;

public class DataConnectionSavedEvent(DataConnection dataConnection) : IEvent
{
    public DataConnection DataConnection { get; } = dataConnection;
}
