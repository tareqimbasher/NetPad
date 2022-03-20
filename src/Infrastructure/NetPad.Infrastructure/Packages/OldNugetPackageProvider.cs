using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NetPad.Utilities;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Settings = NetPad.Configuration.Settings;

namespace NetPad.Packages
{
    [Obsolete("Old implementation. Will be deleted soon.")]
    public class OldNugetPackageProvider : IPackageProvider
    {
        private readonly Settings _settings;
        private const string NUGET_API_URI = "https://api.nuget.org/v3/index.json";

        public OldNugetPackageProvider(Settings settings)
        {
            _settings = settings;
        }

        public virtual Task<HashSet<string>> GetCachedPackageAssembliesAsync(string packageId, string packageVersion)
        {
            var nugetCacheDir = new DirectoryInfo(GetNugetCacheDirectoryPath());

            var packageDir = nugetCacheDir.GetDirectories()
                .FirstOrDefault(d => d.Name == packageId);
            if (packageDir == null) throw new Exception("Package directory not found.");

            var versionDir = packageDir.GetDirectories()
                .FirstOrDefault(d => d.Name == packageVersion);
            if (versionDir == null) throw new Exception("Package version directory not found.");

            var libDir = versionDir.GetDirectories()
                .FirstOrDefault(d => d.Name == "lib");
            if (libDir == null) throw new Exception("Package version lib directory not found.");

            var netstandard2Dir = libDir.GetDirectories()
                .FirstOrDefault(d => d.Name == "netstandard2.0");
            if (netstandard2Dir == null) throw new Exception("No netstandard2.0 directory found for package.");

            string? assemblyPath;

            var dlls = netstandard2Dir.GetFiles("*.dll");
            if (dlls.Length == 1)
                assemblyPath = dlls.First().FullName;

            assemblyPath = dlls
                .FirstOrDefault(d => d.Name.Equals($"{packageId}.dll", StringComparison.OrdinalIgnoreCase))?
                .FullName;

            if (assemblyPath == null)
                throw new Exception("Could not determine main assembly path.");

            return Task.FromResult(new HashSet<string> { assemblyPath });
        }

        public virtual async Task<CachedPackage[]> GetCachedPackagesAsync(bool loadMetadata = false)
        {
            var nugetCacheDir = new DirectoryInfo(GetNugetCacheDirectoryPath());
            using var sourceCacheContext = new SourceCacheContext();

            var cachedPackages = nugetCacheDir.GetDirectories()
                .SelectMany(d =>
                {
                    var versions = new List<CachedPackage>();

                    var versionDirs = d.GetDirectories()
                        .Where(v => Version.TryParse(v.Name, out _))
                        .ToArray();

                    foreach (var versionDir in versionDirs)
                    {
                        versions.Add(new CachedPackage
                        {
                            PackageId = d.Name,
                            DirectoryPath = versionDir.FullName,
                            Version = versionDir.Name
                        });
                    }

                    return versions;
                })
                .ToArray();

            if (loadMetadata)
            {
                var repository = GetSourceRepository();
                var resource = await repository.GetResourceAsync<PackageMetadataResource>();

                await cachedPackages.ForEachAsync(5, async cp =>
                {
                    var metadata = await resource.GetMetadataAsync(
                        new PackageIdentity(cp.PackageId, new NuGetVersion(cp.Version)),
                        sourceCacheContext,
                        NullLogger.Instance,
                        CancellationToken.None);

                    await MapAsync(metadata, cp);
                });
            }

            return cachedPackages.OrderBy(p => p.Title ?? p.PackageId).ToArray();
        }

        public async Task<HashSet<string>> GetPackageAndDependantAssembliesAsync(string packageId, string packageVersion)
        {
            throw new NotImplementedException();
        }

        public virtual Task DeleteCachedPackageAsync(string packageId, string packageVersion)
        {
            var nugetPackageDirectory = new DirectoryInfo(GetNugetCacheDirectoryPath());

            var packageDir = nugetPackageDirectory.GetDirectories()
                .FirstOrDefault(d => d.Name == packageId);

            if (packageDir != null)
            {
                var versionDir = packageDir.GetDirectories()
                    .FirstOrDefault(d => d.Name == packageVersion);

                if (versionDir != null)
                {
                    versionDir.Delete(recursive: true);

                    if (packageDir.GetDirectories().Length == 0)
                        packageDir.Delete();
                }
            }

            return Task.CompletedTask;
        }

        public virtual async Task<PackageMetadata[]> SearchPackagesAsync(
            string? term,
            int skip,
            int take,
            bool includePrerelease,
            CancellationToken? cancellationToken = null)
        {
            if (skip < 0) skip = 0;
            if (take < 0) take = 0;
            else if (take > 200) take = 200;

            var repository = GetSourceRepository();
            var searchResource = await repository.GetResourceAsync<PackageSearchResource>().ConfigureAwait(false);

            var filter = new SearchFilter(includePrerelease);
            var searchResults = await searchResource.SearchAsync(
                term,
                filter,
                skip,
                take,
                NullLogger.Instance,
                cancellationToken ?? CancellationToken.None
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

            return orderedSearchResults.Select(r => r.Package).ToArray();
        }

        public virtual async Task InstallPackageAsync(string packageId, string packageVersion)
        {
            var repository = GetSourceRepository();
            FindPackageByIdResource resource = await repository.GetResourceAsync<FindPackageByIdResource>().ConfigureAwait(false);
            var version = new NuGetVersion(packageVersion);

            await using var packageStream = new MemoryStream();
            using var sourceCacheContext = new SourceCacheContext();

            await resource.CopyNupkgToStreamAsync(
                packageId,
                new NuGetVersion(version),
                packageStream,
                sourceCacheContext,
                NullLogger.Instance,
                CancellationToken.None
            ).ConfigureAwait(false);

            var dir = Path.Combine(GetNugetCacheDirectoryPath(), packageId, version.ToString());
            Directory.CreateDirectory(dir);

            using PackageArchiveReader packageReader = new PackageArchiveReader(packageStream);

            await File.WriteAllTextAsync(Path.Combine(dir, "nuspec.xml"), packageReader.NuspecReader.Xml?.ToString());

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

        public async Task PurgePackageCacheAsync()
        {
            throw new NotImplementedException();
        }

        private SourceRepository GetSourceRepository()
        {
            var providers = new List<Lazy<INuGetResourceProvider>>();
            providers.AddRange(Repository.Provider.GetCoreV3());

            var packageSource = new PackageSource(NUGET_API_URI);
            return new SourceRepository(packageSource, providers);
        }

        protected string GetNugetCacheDirectoryPath()
        {
            var path = Path.Combine(_settings.PackageCacheDirectoryPath, "NuGet");
            return Directory.CreateDirectory(path).FullName;
        }

        protected async Task MapAsync(IPackageSearchMetadata searchMetadata, PackageMetadata packageMetadata)
        {
            packageMetadata.PackageId = searchMetadata.Identity.Id;
            packageMetadata.Title = searchMetadata.Title;
            packageMetadata.Authors = searchMetadata.Authors;
            packageMetadata.Description = searchMetadata.Description;
            packageMetadata.IconUrl = searchMetadata.IconUrl;
            packageMetadata.ProjectUrl = searchMetadata.ProjectUrl;
            packageMetadata.PackageDetailsUrl = searchMetadata.PackageDetailsUrl;
            packageMetadata.LicenseUrl = searchMetadata.LicenseUrl;
            packageMetadata.ReadmeUrl = searchMetadata.ReadmeUrl;
            packageMetadata.ReportAbuseUrl = searchMetadata.ReportAbuseUrl;
            packageMetadata.DownloadCount = searchMetadata.DownloadCount;
            packageMetadata.PublishedDate = searchMetadata.Published?.UtcDateTime;
            // packageMetadata.Dependencies = searchMetadata.DependencySets.SelectMany(ds => ds.Packages.Select(p =>
            //     {
            //         var str = $"{p.Id} ";
            //         var vRange = p.VersionRange;
            //
            //         if (vRange.HasLowerBound || vRange.HasUpperBound)
            //         {
            //             var range = "";
            //
            //             if (vRange.HasUpperBound)
            //                 range += $"{vRange.MaxVersion} <{(vRange.IsMaxInclusive ? "=" : "")}";
            //
            //             if (vRange.HasLowerBound)
            //             {
            //                 if (vRange.HasUpperBound) range += " | ";
            //                 range += $"{vRange.MinVersion} >{(vRange.IsMinInclusive ? "=" : "")}";
            //             }
            //
            //             str += $"({range})";
            //         }
            //
            //         return str;
            //     }))
            //     .ToArray();

            packageMetadata.Dependencies = searchMetadata.DependencySets.Select(dg =>
                    $"{dg.TargetFramework}\n{dg.Packages.Select(p => $"{p.Id} {p.VersionRange}").JoinToString("\n")}")
                .ToArray();

            if (packageMetadata.Version == null)
            {
                packageMetadata.Version = (await searchMetadata.GetVersionsAsync().ConfigureAwait(false))?
                    .LastOrDefault()?
                    .Version.Version.ToString();
            }
        }
    }
}
