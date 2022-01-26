using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NetPad.Common;
using NetPad.Extensions;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NetPad.Packages
{
    public class NugetPackageProvider : IPackageProvider
    {
        private readonly Settings _settings;
        private const string NUGET_API_URI = "https://api.nuget.org/v3/index.json";

        public NugetPackageProvider(Settings settings)
        {
            _settings = settings;
        }

        public async Task<CachedPackage[]> GetCachedPackagesAsync(bool loadMetadata = false)
        {
            string nugetCacheDirPath = GetNugetCacheDirectoryPath();

            var cachedPackages = Directory.GetDirectories(nugetCacheDirPath)
                .Select(d =>
                {
                    var versions = Directory.GetDirectories(d)
                        .Where(v => Version.TryParse(Path.GetFileName(v), out _))
                        .Select(v => Path.GetFileName(v))
                        .ToArray();

                    return new CachedPackage(Path.GetFileName(d), d, versions);
                })
                .ToArray();

            if (loadMetadata)
            {
                var repository = GetSourceRepository();
                var resource = await repository.GetResourceAsync<PackageMetadataResource>();
                //var resource2 = await repository.GetResourceAsync<NuGet.Protocol.Core.Types.PackageSearchResource>().ConfigureAwait(false);
                // resource2.

                await cachedPackages.ForEachAsync(5, async cp =>
                {
                    cp.SetPackageMetadata(new PackageMetadata());

                    var metadata = await resource.GetMetadataAsync(
                        new PackageIdentity(cp.PackageId, new NuGetVersion(cp.Versions.First())),
                        new SourceCacheContext(),
                        NullLogger.Instance,
                        CancellationToken.None);

                    await MapAsync(metadata, cp.PackageMetadata!);
                });
            }

            return cachedPackages;
        }

        public async Task<PackageMetadata[]> SearchPackagesAsync(
            string term,
            int skip,
            int take,
            bool includePrerelease,
            CancellationToken? token = null)
        {
            //return JsonSerializer.Deserialize<PackageMetadata[]>(File.ReadAllText("/home/tips/searchresults.json"));


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

            await orderedSearchResults.ForEachAsync(1, async orderedSearchResult =>
            {
                var result = orderedSearchResult.Result;
                var package = orderedSearchResult.Package;

                await MapAsync(result, package);
            });

            var r = orderedSearchResults.Select(r => r.Package).ToArray();

            //File.WriteAllText("/home/tips/searchresults.json", JsonSerializer.Serialize(r));

            return r;
        }

        public async Task DownloadPackageAsync(string packageId, string packageVersion)
        {
            var repository = GetSourceRepository();
            FindPackageByIdResource resource = await repository.GetResourceAsync<FindPackageByIdResource>().ConfigureAwait(false);
            var version = new NuGetVersion(packageVersion);

            await using var packageStream = new MemoryStream();

            await resource.CopyNupkgToStreamAsync(
                packageId,
                new NuGetVersion(version),
                packageStream,
                new SourceCacheContext(),
                NullLogger.Instance,
                CancellationToken.None
            ).ConfigureAwait(false);

            var dir = Path.Combine(GetNugetCacheDirectoryPath(), packageId, version.ToString());
            Directory.CreateDirectory(dir);

            using PackageArchiveReader packageReader = new PackageArchiveReader(packageStream);
            var frameworkGroups = await packageReader.GetReferenceItemsAsync(CancellationToken.None).ConfigureAwait(false);

            foreach (var frameworkGroup in frameworkGroups)
            {
                foreach (var itemPath in frameworkGroup.Items)
                {
                    packageReader.ExtractFile(
                        itemPath,
                        Path.Combine(dir, itemPath.Trim('/')),
                        NullLogger.Instance);
                }
            }
        }

        private SourceRepository GetSourceRepository()
        {
            var providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3());

            var packageSource = new PackageSource(NUGET_API_URI);
            return new SourceRepository(packageSource, providers);
        }

        private string GetNugetCacheDirectoryPath()
        {
            var path = Path.Combine(_settings.PackageCacheDirectoryPath, "NuGet");
            return Directory.CreateDirectory(path).FullName;
        }

        private async Task MapAsync(IPackageSearchMetadata searchMetadata, PackageMetadata packageMetadata)
        {
            packageMetadata.PackageId = searchMetadata.Identity.Id;
            packageMetadata.Title = searchMetadata.Title;
            packageMetadata.Authors = searchMetadata.Authors;
            packageMetadata.Description = searchMetadata.Description;
            packageMetadata.IconUrl = searchMetadata.IconUrl;
            packageMetadata.ProjectUrl = searchMetadata.ProjectUrl;
            packageMetadata.DownloadCount = searchMetadata.DownloadCount;
            packageMetadata.Versions = (await searchMetadata.GetVersionsAsync().ConfigureAwait(false))?
                .Select(v => v.Version.Version.ToString())
                .ToArray() ?? Array.Empty<string>();
        }
    }
}
