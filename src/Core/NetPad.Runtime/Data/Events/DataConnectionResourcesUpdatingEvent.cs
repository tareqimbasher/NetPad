using NetPad.DotNet;
using NetPad.Events;

namespace NetPad.Data.Events;

public class DataConnectionResourcesUpdatingEvent(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion) : IEvent
{
    public DataConnection DataConnection { get; } = dataConnection;
    public DotNetFrameworkVersion TargetFrameworkVersion { get; } = targetFrameworkVersion;
}
