using NetPad.DotNet;
using NetPad.Events;

namespace NetPad.Data.Events;

public class DataConnectionResourcesUpdatingEvent(DataConnection dataConnection, DotNetFrameworkVersion dotNetFrameworkVersion) : IEvent
{
    public DataConnection DataConnection { get; } = dataConnection;
    public DotNetFrameworkVersion DotNetFrameworkVersion { get; } = dotNetFrameworkVersion;
}
