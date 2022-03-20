using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetPad.Packages;

namespace NetPad.Tests.Services;

public class NullPackageProvider : IPackageProvider
{
    public async Task<HashSet<string>> GetCachedPackageAssembliesAsync(string packageId, string packageVersion)
    {
        throw new NotImplementedException();
    }

    public Task<CachedPackage[]> GetCachedPackagesAsync(bool loadMetadata = false)
    {
        return Task.FromResult(Array.Empty<CachedPackage>());
    }

    public async Task<HashSet<string>> GetPackageAndDependantAssembliesAsync(string packageId, string packageVersion)
    {
        throw new NotImplementedException();
    }

    public Task DeleteCachedPackageAsync(string packageId, string packageVersion)
    {
        return Task.CompletedTask;
    }

    public Task<PackageMetadata[]> SearchPackagesAsync(string? term, int skip, int take, bool includePrerelease, CancellationToken? cancellationToken = null)
    {
        return Task.FromResult(Array.Empty<PackageMetadata>());
    }

    public Task InstallPackageAsync(string packageId, string packageVersion)
    {
        return Task.CompletedTask;
    }

    public async Task PurgePackageCacheAsync()
    {
        throw new NotImplementedException();
    }
}
