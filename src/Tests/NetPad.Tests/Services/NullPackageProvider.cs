using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetPad.Packages;

namespace NetPad.Tests.Services;

public class NullPackageProvider : IPackageProvider
{
    public Task<HashSet<string>> GetCachedPackageAssembliesAsync(string packageId, string packageVersion)
    {
        throw new NotImplementedException();
    }

    public Task<CachedPackage[]> GetCachedPackagesAsync(bool loadMetadata = false)
    {
        return Task.FromResult(Array.Empty<CachedPackage>());
    }

    public Task<CachedPackage[]> GetExplicitlyInstalledCachedPackagesAsync(bool loadMetadata = false)
    {
        throw new NotImplementedException();
    }

    public Task<HashSet<string>> GetPackageAndDependanciesAssembliesAsync(string packageId, string packageVersion)
    {
        throw new NotImplementedException();
    }

    public Task<string[]> GetPackageVersionsAsync(string packageId)
    {
        throw new NotImplementedException();
    }

    public Task DeleteCachedPackageAsync(string packageId, string packageVersion)
    {
        return Task.CompletedTask;
    }

    public Task<PackageMetadata[]> SearchPackagesAsync(
        string? term,
        int skip,
        int take,
        bool includePrerelease,
        bool loadMetadata = false,
        CancellationToken? cancellationToken = null)
    {
        return Task.FromResult(Array.Empty<PackageMetadata>());
    }

    public Task InstallPackageAsync(string packageId, string packageVersion)
    {
        return Task.CompletedTask;
    }

    public Task<PackageInstallInfo?> GetPackageInstallInfoAsync(string packageId, string packageVersion)
    {
        return Task.FromResult<PackageInstallInfo?>(null);
    }

    public Task PurgePackageCacheAsync()
    {
        throw new NotImplementedException();
    }
}
