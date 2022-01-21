using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

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
        private const string NUGET_API_URI = "https://api.nuget.org/v3/index.json";

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

            var repository = GetSourceRepository();
            PackageSearchResource resource = await repository.GetResourceAsync<PackageSearchResource>().ConfigureAwait(false);

            var filter = new SearchFilter(includePrerelease);
            var searchResults = await resource.SearchAsync(
                term,
                filter,
                skip,
                take,
                NullLogger.Instance,
                token ?? CancellationToken.None
            ).ConfigureAwait(false);

            var orderedSearchResults = searchResults.Select(r => new
            {
                Result = r,
                Package = new PackageMetadata()
            }).ToArray();

            await orderedSearchResults.ForEachAsync(10, async orderedSearchResult =>
            {
                var result = orderedSearchResult.Result;
                var package = orderedSearchResult.Package;

                package.Id = result.Identity.Id;
                package.Title = result.Title;
                package.Authors = result.Authors;
                package.Description = result.Description;
                package.IconUrl = result.IconUrl;
                package.ProjectUrl = result.ProjectUrl;
                package.DownloadCount = result.DownloadCount;
                package.Versions = (await result.GetVersionsAsync().ConfigureAwait(false))
                    .Select(v => v.Version.Version.ToString()).ToArray();
            });

            return orderedSearchResults.Select(r => r.Package).ToArray();
        }

        public async Task DownloadPackageAsync(PackageMetadata package, string version)
        {
            var repository = GetSourceRepository();
            FindPackageByIdResource resource = await repository.GetResourceAsync<FindPackageByIdResource>().ConfigureAwait(false);

            using var packageStream = new MemoryStream();

            await resource.CopyNupkgToStreamAsync(
                package.Id,
                new NuGetVersion(version),
                packageStream,
                new SourceCacheContext(),
                NullLogger.Instance,
                CancellationToken.None
            ).ConfigureAwait(false);

            using PackageArchiveReader packageReader = new PackageArchiveReader(packageStream);
            var refItems = await packageReader.GetReferenceItemsAsync(CancellationToken.None).ConfigureAwait(false);

            foreach (var item in refItems)
            {
                //item.TargetFramework.
            }
        }

        private SourceRepository GetSourceRepository()
        {
            var providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3());

            var packageSource = new PackageSource(NUGET_API_URI);
            return new SourceRepository(packageSource, providers);
        }
    }

    public static class Ext
    {
        public static Task ForEachAsync<T>(this IEnumerable<T> collection, int dop, Func<T, Task> body)
        {
            return Task.WhenAll(
                from partition in Partitioner.Create(collection).GetPartitions(dop)
                select Task.Run(async delegate
                {
                    using (partition)
                        while (partition.MoveNext())
                            await body(partition.Current).ContinueWith(t =>
                            {
                                // observe exceptions
                            });
                })
            );
        }
    }
}
