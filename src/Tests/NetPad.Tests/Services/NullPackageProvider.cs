using System;
using System.Threading;
using System.Threading.Tasks;
using NetPad.Packages;

namespace NetPad.Tests.Services;

public class NullPackageProvider : IPackageProvider
{
    public Task<string> GetCachedPackageAssemblyPathAsync(string packageId, string packageVersion)
    {
        return Task.FromResult(string.Empty);
    }

    public Task<CachedPackage[]> GetCachedPackagesAsync(bool loadMetadata = false)
    {
        return Task.FromResult(Array.Empty<CachedPackage>());
    }

    public Task DeleteCachedPackageAsync(string packageId, string packageVersion)
    {
        return Task.CompletedTask;
    }

    public Task<PackageMetadata[]> SearchPackagesAsync(string? term, int skip, int take, bool includePrerelease, CancellationToken? token = null)
    {
        return Task.FromResult(Array.Empty<PackageMetadata>());
    }

    public Task DownloadPackageAsync(string packageId, string packageVersion)
    {
        return Task.CompletedTask;
    }
}
