using NetPad.DotNet;

namespace NetPad.Packages;

/// <summary>
/// Installs packages and provides access to cached (installed) packages and their assets.
/// </summary>
public interface IPackageProvider
{
    /// <summary>
    /// Searches for packages.
    /// </summary>
    /// <param name="term">Term to search for.</param>
    /// <param name="skip">Skip, used for paging.</param>
    /// <param name="take">Take, used for paging.</param>
    /// <param name="includePrerelease">Whether to include pre-release package version in search results.</param>
    /// <param name="loadMetadata">Whether to load metadata or not. If false, some basic metadata will
    /// still be loaded from the search operation.</param>
    /// <param name="cancellationToken">Cancellation cancellationToken.</param>
    Task<PackageMetadata[]> SearchPackagesAsync(
        string? term,
        int skip,
        int take,
        bool includePrerelease,
        bool loadMetadata = false,
        CancellationToken? cancellationToken = null);

    /// <summary>
    /// Gets all versions of a package.
    /// </summary>
    /// <param name="packageId">Package ID</param>
    /// <param name="includePrerelease">Whether to include pre-release versions.</param>
    Task<string[]> GetPackageVersionsAsync(string packageId, bool includePrerelease);

    /// <summary>
    /// Gets metadata for a set set of packages.
    /// </summary>
    Task<Dictionary<PackageIdentity, PackageMetadata?>> GetExtendedMetadataAsync(
        IEnumerable<PackageIdentity> packageIdentities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Installs a package and adds it to cache.
    /// </summary>
    /// <param name="packageId">Package ID</param>
    /// <param name="packageVersion">Package Version</param>
    /// <param name="dotNetFrameworkVersion">The .NET framework version to target.</param>
    Task InstallPackageAsync(string packageId, string packageVersion, DotNetFrameworkVersion dotNetFrameworkVersion);

    /// <summary>
    /// Gets install info for a specific package ID and version if that package is installed.
    /// </summary>
    /// <param name="packageId">Package ID</param>
    /// <param name="packageVersion">Package Version</param>
    /// <returns>Install info for the specified package if it is installed, null otherwise.</returns>
    Task<PackageInstallInfo?> GetPackageInstallInfoAsync(string packageId, string packageVersion);

    /// <summary>
    /// Gets a listing of all cached (installed) packages.
    /// </summary>
    /// <param name="loadMetadata">Whether to load metadata or not. If false, some basic metadata will
    /// still be loaded from installed package.</param>
    Task<CachedPackage[]> GetCachedPackagesAsync(bool loadMetadata = false);

    /// <summary>
    /// Gets a listing cached (installed) packages that were explicitly installed.
    /// </summary>
    /// <param name="loadMetadata">Whether to load metadata or not. If false, some basic metadata will
    /// still be loaded from installed package.</param>
    /// <returns></returns>
    Task<CachedPackage[]> GetExplicitlyInstalledCachedPackagesAsync(bool loadMetadata = false);

    /// <summary>
    /// Gets package asset files.
    /// </summary>
    /// <param name="packageId">Package ID</param>
    /// <param name="packageVersion">Package Version</param>
    /// <param name="dotNetFrameworkVersion">The .NET framework version to target.</param>
    Task<HashSet<PackageAsset>> GetCachedPackageAssetsAsync(string packageId, string packageVersion, DotNetFrameworkVersion dotNetFrameworkVersion);

    /// <summary>
    /// Gets package assets including any assets of package dependencies.
    /// </summary>
    /// <param name="packageId">Package ID</param>
    /// <param name="packageVersion">Package Version</param>
    /// <param name="dotNetFrameworkVersion">The .NET framework version to target.</param>
    Task<HashSet<PackageAsset>> GetRecursivePackageAssetsAsync(string packageId, string packageVersion, DotNetFrameworkVersion dotNetFrameworkVersion);


    /// <summary>
    /// Deletes a package from the cache.
    /// </summary>
    /// <param name="packageId">Package ID</param>
    /// <param name="packageVersion">Package Version</param>
    Task DeleteCachedPackageAsync(string packageId, string packageVersion);

    /// <summary>
    /// Deletes all cached packages.
    /// </summary>
    Task PurgePackageCacheAsync();
}
