using System.Text.Json.Serialization;
using NetPad.Events;

namespace NetPad.Data.Events;

public class DataConnectionResourcesUpdatedEvent(
    DataConnection dataConnection,
    DataConnectionResources resources,
    DataConnectionResourceComponent updatedComponent)
    : IEvent
{
    public DataConnection DataConnection { get; } = dataConnection;

    [JsonIgnore] public DataConnectionResources Resources { get; } = resources;

    public DataConnectionResourceComponent UpdatedComponent { get; } = updatedComponent;
}
