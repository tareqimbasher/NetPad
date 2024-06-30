using NetPad.DotNet;

namespace NetPad.Data;

public class DataConnectionResources(DataConnection dataConnection, DateTime recentAsOf)
{
    public DataConnection DataConnection { get; } = dataConnection;
    public DateTime RecentAsOf { get; private set; } = recentAsOf;
    public Task<DataConnectionSourceCode>? SourceCode { get; set; }
    public Task<AssemblyImage?>? Assembly { get; set; }
    public Task<Reference[]>? RequiredReferences { get; set; }

    public DataConnectionResources UpdateRecentAsOf(DateTime dateTime)
    {
        RecentAsOf = dateTime;
        return this;
    }

    public DataConnectionResources UpdateFrom(DataConnectionResources other)
    {
        RecentAsOf = other.RecentAsOf;
        SourceCode = other.SourceCode;
        Assembly = other.Assembly;
        RequiredReferences = other.RequiredReferences;
        return this;
    }
}
