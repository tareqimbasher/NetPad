using System.Threading;
using System.Threading.Tasks;

namespace NetPad.Packages
{
    public interface IPackageProvider
    {
        Task<string> GetCachedPackageAssemblyPathAsync(string packageId, string packageVersion);

        Task<CachedPackage[]> GetCachedPackagesAsync(bool loadMetadata = false);

        Task DeleteCachedPackageAsync(string packageId, string packageVersion);

        Task<PackageMetadata[]> SearchPackagesAsync(
            string? term,
            int skip,
            int take,
            bool includePrerelease,
            CancellationToken? token = null);

        Task DownloadPackageAsync(string packageId, string packageVersion);
    }
}
