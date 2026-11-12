using NetPad.DotNet;
using NetPad.Events;

namespace NetPad.Data.Events;

public class DataConnectionResourcesUpdateFailedEvent(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion, Exception? exception)
    : IEvent
{
    public DataConnection DataConnection { get; } = dataConnection;
    public DotNetFrameworkVersion TargetFrameworkVersion { get; } = targetFrameworkVersion;
    public string? Error { get; } = exception?.Message;
}
