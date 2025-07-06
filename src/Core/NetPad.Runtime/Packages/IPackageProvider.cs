using NetPad.DotNet;

namespace NetPad.Packages;

/// <summary>
/// Downloads (installs) and caches packages and provides access to cached (downloaded) packages and their assets.
/// </summary>
public interface IPackageProvider
{
    /// <summary>
    /// Searches for packages.
    /// </summary>
    /// <param name="term">The term to search for.</param>
    /// <param name="skip">How many items to skip; used for paging.</param>
    /// <param name="take">How many items to take, used for paging.</param>
    /// <param name="includePrerelease">Whether to include pre-release package versions in search results.</param>
    /// <param name="loadMetadata">Whether to load metadata or not. If false, some basic metadata will
    /// still be loaded from the search operation.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the search.</param>
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
    /// <param name="packageId">The ID of the package.</param>
    /// <param name="includePrerelease">Whether to include pre-release versions.</param>
    Task<string[]> GetPackageVersionsAsync(string packageId, bool includePrerelease);

    /// <summary>
    /// Gets metadata for a collection of packages.
    /// </summary>
    Task<Dictionary<PackageIdentity, PackageMetadata?>> GetExtendedMetadataAsync(
        IEnumerable<PackageIdentity> packageIdentities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Installs (downloads) a package and adds it to the cache.
    /// </summary>
    /// <param name="packageId">The ID of the package.</param>
    /// <param name="packageVersion">The version of the package.</param>
    /// <param name="dotNetFrameworkVersion">The .NET framework version to target.</param>
    Task InstallPackageAsync(string packageId, string packageVersion, DotNetFrameworkVersion dotNetFrameworkVersion);

    /// <summary>
    /// Gets install info for a specific package ID and version if that package is installed.
    /// </summary>
    /// <param name="packageId">The ID of the package.</param>
    /// <param name="packageVersion">The version of the package.</param>
    /// <returns>Install info for the specified package if it is installed, null otherwise.</returns>
    Task<PackageInstallInfo?> GetPackageInstallInfoAsync(string packageId, string packageVersion);

    /// <summary>
    /// Gets a listing of all cached packages.
    /// </summary>
    /// <param name="loadMetadata">Whether to load metadata or not. If false, some basic metadata will
    /// still be loaded from cached package.</param>
    Task<CachedPackage[]> GetCachedPackagesAsync(bool loadMetadata = false);

    /// <summary>
    /// Gets a listing of cached packages that were explicitly installed.
    /// </summary>
    /// <param name="loadMetadata">Whether to load metadata or not. If false, some basic metadata will
    /// still be loaded from cached package.</param>
    /// <returns></returns>
    Task<CachedPackage[]> GetExplicitlyInstalledCachedPackagesAsync(bool loadMetadata = false);

    /// <summary>
    /// Gets package asset files.
    /// </summary>
    /// <param name="packageId">The ID of the package.</param>
    /// <param name="packageVersion">The version of the package.</param>
    /// <param name="dotNetFrameworkVersion">The .NET framework version to target.</param>
    Task<HashSet<PackageAsset>> GetCachedPackageAssetsAsync(
        string packageId,
        string packageVersion,
        DotNetFrameworkVersion dotNetFrameworkVersion);

    /// <summary>
    /// Gets package assets including the assets of dependency packages.
    /// </summary>
    /// <param name="packageId">The ID of the package.</param>
    /// <param name="packageVersion">The version of the package.</param>
    /// <param name="dotNetFrameworkVersion">The .NET framework version to target.</param>
    Task<HashSet<PackageAsset>> GetRecursivePackageAssetsAsync(
        string packageId,
        string packageVersion,
        DotNetFrameworkVersion dotNetFrameworkVersion);


    /// <summary>
    /// Deletes a package from the cache.
    /// </summary>
    /// <param name="packageId">The ID of the package.</param>
    /// <param name="packageVersion">The version of the package.</param>
    Task DeleteCachedPackageAsync(string packageId, string packageVersion);

    /// <summary>
    /// Deletes all cached packages.
    /// </summary>
    Task PurgePackageCacheAsync();
}
