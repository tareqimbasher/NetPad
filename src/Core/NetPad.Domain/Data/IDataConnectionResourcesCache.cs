using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetPad.DotNet;

namespace NetPad.Data;

public interface IDataConnectionResourcesCache
{
    Dictionary<DotNetFrameworkVersion, DataConnectionResources>? GetCached(Guid dataConnectionId);
    bool HasCachedResources(Guid dataConnectionId, DotNetFrameworkVersion targetFrameworkVersion);
    void RemoveCachedResources(Guid dataConnectionId, DotNetFrameworkVersion targetFrameworkVersion);
    Task<AssemblyImage?> GetAssemblyAsync(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion);
    Task<DataConnectionSourceCode> GetSourceGeneratedCodeAsync(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion);
    Task<Reference[]> GetRequiredReferencesAsync(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion);
}
