using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetPad.Compilation;
using NetPad.Data;
using NetPad.DotNet;

namespace NetPad.Tests.Services;

public class NullDataConnectionResourcesCache : IDataConnectionResourcesCache
{
    public bool HasCachedResources(Guid dataConnectionId)
    {
        return false;
    }

    public void RemoveCachedResources(Guid dataConnectionId)
    {
    }

    public Task<byte[]?> GetAssemblyAsync(DataConnection dataConnection)
    {
        return Task.FromResult<byte[]?>(null);
    }

    public Task<SourceCodeCollection> GetSourceGeneratedCodeAsync(DataConnection dataConnection)
    {
        return Task.FromResult(new SourceCodeCollection());
    }

    public Task<IEnumerable<Reference>> GetRequiredReferencesAsync(DataConnection dataConnection)
    {
        return Task.FromResult<IEnumerable<Reference>>(Array.Empty<Reference>());
    }
}
