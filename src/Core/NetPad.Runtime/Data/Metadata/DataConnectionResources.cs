using NetPad.DotNet;

namespace NetPad.Data.Metadata;

public class DataConnectionResources(DataConnection dataConnection, DateTime recentAsOf)
{
    public DataConnection DataConnection { get; } = dataConnection;
    public DateTime RecentAsOf { get; private set; } = recentAsOf;

    public DataConnectionSourceCode? SourceCode { get; set; }
    public AssemblyImage? Assembly { get; set; }
    public Reference[]? RequiredReferences { get; set; }
    public DatabaseStructure? DatabaseStructure { get; set; }

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
        DatabaseStructure = other.DatabaseStructure;
        return this;
    }
}
