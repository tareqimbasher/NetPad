using NetPad.DotNet;
using NetPad.Events;

namespace NetPad.Data.Events;

public class DataConnectionResourcesUpdateFailedEvent(DataConnection dataConnection, DotNetFrameworkVersion dotNetFrameworkVersion, Exception? exception)
    : IEvent
{
    public DataConnection DataConnection { get; } = dataConnection;
    public DotNetFrameworkVersion DotNetFrameworkVersion { get; } = dotNetFrameworkVersion;
    public string? Error { get; } = exception?.Message;
}
