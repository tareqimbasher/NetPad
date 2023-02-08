using System;
using System.Threading.Tasks;
using NetPad.DotNet;

namespace NetPad.Data;

public interface IDataConnectionResourcesCache
{
    bool HasCachedResources(Guid dataConnectionId);
    void RemoveCachedResources(Guid dataConnectionId);
    Task<AssemblyImage?> GetAssemblyAsync(DataConnection dataConnection);
    Task<DataConnectionSourceCode> GetSourceGeneratedCodeAsync(DataConnection dataConnection);
    Task<Reference[]> GetRequiredReferencesAsync(DataConnection dataConnection);
}
