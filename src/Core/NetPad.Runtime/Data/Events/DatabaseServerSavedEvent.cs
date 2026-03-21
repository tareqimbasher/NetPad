using NetPad.Events;

namespace NetPad.Data.Events;

public class DatabaseServerSavedEvent(DatabaseServerConnection server) : IEvent
{
    public DatabaseServerConnection Server { get; } = server;
}
