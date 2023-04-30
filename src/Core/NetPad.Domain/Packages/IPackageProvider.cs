using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NetPad.Packages;

/// <summary>
/// Installs packages and provides access to cached (installed) packages and their assets.
/// </summary>
public interface IPackageProvider
{
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
    /// Deletes all cached packages.
    /// </summary>
    Task PurgePackageCacheAsync();

    /// <summary>
    /// Gets package assemblies.
    /// </summary>
    /// <param name="packageId">Package ID</param>
    /// <param name="packageVersion">Package Version</param>
    Task<HashSet<string>> GetCachedPackageAssembliesAsync(string packageId, string packageVersion);

    /// <summary>
    /// Gets package assemblies including any dependant assemblies.
    /// </summary>
    /// <param name="packageId">Package ID</param>
    /// <param name="packageVersion">Package Version</param>
    Task<HashSet<string>> GetPackageAndDependanciesAssembliesAsync(string packageId, string packageVersion);

    /// <summary>
    /// Gets all versions of a package.
    /// </summary>
    /// <param name="packageId">Package ID</param>
    Task<string[]> GetPackageVersionsAsync(string packageId);

    /// <summary>
    /// Deletes a package from the cache.
    /// </summary>
    /// <param name="packageId">Package ID</param>
    /// <param name="packageVersion">Package Version</param>
    Task DeleteCachedPackageAsync(string packageId, string packageVersion);

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
    /// Installs a package and adds it to cache.
    /// </summary>
    /// <param name="packageId">Package ID</param>
    /// <param name="packageVersion">Package Version</param>
    Task InstallPackageAsync(string packageId, string packageVersion);

    /// <summary>
    /// Gets install info for a specific package ID and version if that package is installed.
    /// </summary>
    /// <param name="packageId">Package ID</param>
    /// <param name="packageVersion">Package Version</param>
    /// <returns>Install info for the specified package if it is installed, null otherwise.</returns>
    Task<PackageInstallInfo?> GetPackageInstallInfoAsync(string packageId, string packageVersion);
}
