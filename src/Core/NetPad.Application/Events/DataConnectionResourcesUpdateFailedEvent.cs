using NetPad.Data;

namespace NetPad.Events;

public class DataConnectionResourcesUpdateFailedEvent : IEvent
{
    public DataConnectionResourcesUpdateFailedEvent(DataConnection dataConnection, DataConnectionResourceComponent failedComponent, Exception? exception)
    {
        DataConnection = dataConnection;
        FailedComponent = failedComponent;
        Error = exception?.Message;
    }

    public DataConnection DataConnection { get; }
    public DataConnectionResourceComponent FailedComponent { get; }
    public string? Error { get; }
}
