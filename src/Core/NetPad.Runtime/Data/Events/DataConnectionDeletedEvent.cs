using NetPad.Events;

namespace NetPad.Data.Events;

public class DataConnectionDeletedEvent(DataConnection dataConnection) : IEvent
{
    public DataConnection DataConnection { get; } = dataConnection;
}
