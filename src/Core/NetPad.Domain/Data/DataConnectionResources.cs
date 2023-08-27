using System;
using System.Threading.Tasks;
using NetPad.DotNet;

namespace NetPad.Data;

public class DataConnectionResources
{
    public DataConnectionResources(DataConnection dataConnection, DateTime recentAsOf)
    {
        DataConnection = dataConnection;
        RecentAsOf = recentAsOf;
    }

    public DataConnection DataConnection { get; }
    public DateTime RecentAsOf { get; private set; }
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
