using NetPad.Events;

namespace NetPad.Data.Events;

public class DataConnectionResourcesUpdateFailedEvent(
    DataConnection dataConnection,
    DataConnectionResourceComponent failedComponent,
    Exception? exception)
    : IEvent
{
    public DataConnection DataConnection { get; } = dataConnection;
    public DataConnectionResourceComponent FailedComponent { get; } = failedComponent;
    public string? Error { get; } = exception?.Message;
}
