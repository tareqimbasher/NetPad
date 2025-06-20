using NetPad.Data;
using NetPad.Data.Metadata;
using NetPad.DotNet;

namespace NetPad.Tests.Services;

public class NullDataConnectionResourcesCache : IDataConnectionResourcesCache
{
    public Task<bool> HasCachedResourcesAsync(Guid dataConnectionId, DotNetFrameworkVersion targetFrameworkVersion)
    {
        return Task.FromResult(false);
    }

    public Task<IList<DotNetFrameworkVersion>> GetCachedDotNetFrameworkVersions(Guid dataConnectionId)
    {
        return Task.FromResult<IList<DotNetFrameworkVersion>>(Array.Empty<DotNetFrameworkVersion>());
    }

    public Task RemoveCachedResourcesAsync(Guid dataConnectionId)
    {
        return Task.CompletedTask;
    }

    public Task RemoveCachedResourcesAsync(Guid dataConnectionId, DotNetFrameworkVersion targetFrameworkVersion)
    {
        return Task.CompletedTask;
    }

    public Task<DataConnectionResources> GetResourcesAsync(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion)
    {
        return Task.FromResult(new DataConnectionResources(dataConnection, DateTime.UtcNow));
    }
}
