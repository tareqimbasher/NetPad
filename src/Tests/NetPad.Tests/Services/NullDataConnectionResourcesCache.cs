using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetPad.Data;
using NetPad.DotNet;

namespace NetPad.Tests.Services;

public class NullDataConnectionResourcesCache : IDataConnectionResourcesCache
{
    public Dictionary<DotNetFrameworkVersion, DataConnectionResources>? GetCached(Guid dataConnectionId)
    {
        return null;
    }

    public bool HasCachedResources(Guid dataConnectionId, DotNetFrameworkVersion targetFrameworkVersion)
    {
        return false;
    }

    public void RemoveCachedResources(Guid dataConnectionId, DotNetFrameworkVersion targetFrameworkVersion)
    {
    }

    public Task<AssemblyImage?> GetAssemblyAsync(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion)
    {
        return Task.FromResult<AssemblyImage?>(null);
    }

    public Task<DataConnectionSourceCode> GetSourceGeneratedCodeAsync(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion)
    {
        return Task.FromResult(new DataConnectionSourceCode());
    }

    public Task<Reference[]> GetRequiredReferencesAsync(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion)
    {
        return Task.FromResult(Array.Empty<Reference>());
    }
}
