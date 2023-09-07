using System;
using System.Threading.Tasks;
using NetPad.Data;
using NetPad.DotNet;

namespace NetPad.Tests.Services;

public class NullDataConnectionResourcesCache : IDataConnectionResourcesCache
{
    public Task<bool> HasCachedResourcesAsync(Guid dataConnectionId, DotNetFrameworkVersion targetFrameworkVersion)
    {
        return Task.FromResult(false);
    }

    public Task RemoveCachedResourcesAsync(Guid dataConnectionId)
    {
        return Task.CompletedTask;
    }

    public Task RemoveCachedResourcesAsync(Guid dataConnectionId, DotNetFrameworkVersion targetFrameworkVersion)
    {
        return Task.CompletedTask;
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
