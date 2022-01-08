using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;

namespace NetPad.Packages
{
    public interface IPackageProvider
    {
        Task<IEnumerable<PackageMetadata>> SearchPackagesAsync(
            string term,
            int skip,
            int take,
            bool includePrerelease,
            CancellationToken? token = null);
    }

    public class PackageProvider : IPackageProvider
    {
        public async Task<IEnumerable<PackageMetadata>> SearchPackagesAsync(
            string term,
            int skip,
            int take,
            bool includePrerelease,
            CancellationToken? token = null)
        {
            if (skip < 0) skip = 0;
            if (take < 0) take = 0;
            else if (take > 200) take = 200;

            var providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3());

            var packageSource = new PackageSource("https://api.nuget.org/v3/index.json");
            var source = new SourceRepository(packageSource, providers);

            var filter = new SearchFilter(includePrerelease);
            var resource = await source.GetResourceAsync<PackageSearchResource>().ConfigureAwait(false);
            var searchResults = await resource.SearchAsync(
                term,
                filter,
                skip,
                take,
                NullLogger.Instance,
                token ?? CancellationToken.None
            ).ConfigureAwait(false);

            var packages = new List<PackageMetadata>();

            foreach (var result in searchResults)
            {
                var package = new PackageMetadata
                {
                    Title = result.Title,
                    Authors = result.Authors,
                    Description = result.Description,
                    IconUrl = result.IconUrl,
                    ProjectUrl = result.ProjectUrl,
                    DownloadCount = result.DownloadCount
                };

                package.Versions = (await result.GetVersionsAsync()).Select(v => v.Version.Version.ToString()).ToArray();
                packages.Add(package);
            }

            return packages;
        }
    }
}
