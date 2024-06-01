using NetPad.DotNet;
using NetPad.Packages;

namespace NetPad.Tests.Services;

public class NullPackageProvider : IPackageProvider
{
    public Task<HashSet<PackageAsset>> GetCachedPackageAssetsAsync(string packageId, string packageVersion, DotNetFrameworkVersion dotNetFrameworkVersion)
    {
        throw new NotImplementedException();
    }

    public Task<HashSet<PackageAsset>> GetRecursivePackageAssetsAsync(string packageId, string packageVersion, DotNetFrameworkVersion dotNetFrameworkVersion)
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

    public Task<string[]> GetPackageVersionsAsync(string packageId, bool includePrerelease)
    {
        throw new NotImplementedException();
    }

    public Task<Dictionary<PackageIdentity, PackageMetadata?>> GetExtendedMetadataAsync(IEnumerable<PackageIdentity> packageIdentities, CancellationToken cancellationToken = default)
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

    public Task InstallPackageAsync(string packageId, string packageVersion, DotNetFrameworkVersion dotNetFrameworkVersion)
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
