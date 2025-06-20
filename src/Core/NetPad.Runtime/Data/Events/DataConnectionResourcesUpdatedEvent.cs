using System.Text.Json.Serialization;
using NetPad.Data.Metadata;
using NetPad.DotNet;
using NetPad.Events;

namespace NetPad.Data.Events;

public class DataConnectionResourcesUpdatedEvent(
    DataConnection dataConnection,
    DotNetFrameworkVersion targetFrameworkVersion,
    DataConnectionResources resources) : IEvent
{
    public DataConnection DataConnection { get; } = dataConnection;
    public DotNetFrameworkVersion TargetFrameworkVersion { get; } = targetFrameworkVersion;

    [JsonIgnore] public DataConnectionResources Resources { get; } = resources;
}
