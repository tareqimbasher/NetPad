using NetPad.Data;

namespace NetPad.Events;

public class DataConnectionResourcesUpdatedEvent : IEvent
{
    public DataConnectionResourcesUpdatedEvent(DataConnection dataConnection, DataConnectionResources resources, UpdatedComponentType updatedComponent)
    {
        DataConnection = dataConnection;
        Resources = resources;
        UpdatedComponent = updatedComponent;
    }

    public DataConnection DataConnection { get; }
    public DataConnectionResources Resources { get; }
    public UpdatedComponentType UpdatedComponent { get; }

    public enum UpdatedComponentType
    {
        SourceCode, Assembly, RequiredReferences
    }
}
