using NetPad.Events;

namespace NetPad.Data.Events;

public class DatabaseServerDeletedEvent(DatabaseServerConnection server) : IEvent
{
    public DatabaseServerConnection Server { get; } = server;
}
