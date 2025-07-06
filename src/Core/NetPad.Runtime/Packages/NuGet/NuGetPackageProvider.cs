using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;
using NetPad.Application;
using NetPad.Common;
using NetPad.DotNet;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Resolver;
using NuGet.Versioning;
using INugetLogger = NuGet.Common.ILogger;
using NuGetNullLogger = NuGet.Common.NullLogger;
using Settings = NetPad.Configuration.Settings;
using NugetPackageIdentity = NuGet.Packaging.Core.PackageIdentity;

namespace NetPad.Packages.NuGet;

public class NuGetPackageProvider(
    Settings settings,
    IAppStatusMessagePublisher appStatusMessagePublisher,
    ILogger<NuGetPackageProvider> logger)
    : IPackageProvider
{
    private const string NugetApiUri = "https://api.nuget.org/v3/index.json";
    private const string PackageInstallInfoFileName = "netpad.json";

    // hostDependencyContext = DependencyContext.Load(hostAssembly);
    // FrameworkName = hostDependencyContext.Target.Framework;
    // TargetFramework = NuGetFramework.ParseFrameworkName(FrameworkName, DefaultFrameworkNameProvider.Instance);

    public async Task<PackageMetadata[]> SearchPackagesAsync(
        string? term,
        int skip,
        int take,
        bool includePrerelease,
        bool loadMetadata = false,
        CancellationToken? cancellationToken = null)
    {
        if (skip < 0) skip = 0;
        if (take < 0) take = 0;
        else if (take > 100) take = 100;

        var filter = new SearchFilter(includePrerelease);
        var packages = new List<PackageMetadata>();

        var resources = GetSourceRepositoryProvider()
            .GetRepositories()
            .Select(r => r.GetResourceAsync<PackageSearchResource>().ConfigureAwait(false));

        foreach (var resource in resources)
        {
            IEnumerable<IPackageSearchMetadata>? searchResults;

            try
            {
                var searchResource = await resource;
                searchResults = await searchResource.SearchAsync(
                    term,
                    filter,
                    skip,
                    take,
                    NuGetNullLogger.Instance,
                    cancellationToken ?? CancellationToken.None
                ).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error searching packages");
                continue;
            }

            foreach (var searchResult in searchResults)
            {
                var metadata = new PackageMetadata(searchResult.Identity.Id, searchResult.Title);
                await MapAsync(searchResult, metadata);
                packages.Add(metadata);
            }

            if (loadMetadata)
            {
                await HydrateMetadataAsync(packages, TimeSpan.FromSeconds(packages.Count * 5));
            }
        }

        return packages.ToArray();
    }

    public async Task<string[]> GetPackageVersionsAsync(string packageId, bool includePrerelease)
    {
        using var sourceCacheContext = new SourceCacheContext();

        foreach (var repository in GetSourceRepositoryProvider().GetRepositories())
        {
            NuGetVersion[] versions;

            try
            {
                var resource = await repository.GetResourceAsync<FindPackageByIdResource>();
                versions = (await resource.GetAllVersionsAsync(
                        packageId,
                        sourceCacheContext,
                        NuGetNullLogger.Instance,
                        CancellationToken.None))
                    .ToArray();
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error getting package versions");
                continue;
            }

            if (versions.Any())
            {
                return versions
                    .Where(v => includePrerelease || !v.IsPrerelease)
                    .Select(v => v.ToString())
                    .ToArray();
            }
        }

        return [];
    }

    public async Task InstallPackageAsync(
        string packageId,
        string packageVersion,
        DotNetFrameworkVersion dotNetFrameworkVersion)
    {
        var packageIdentity = new NugetPackageIdentity(packageId, NuGetVersion.Parse(packageVersion));

        var sourceRepositoryProvider = GetSourceRepositoryProvider();
        var repositories = sourceRepositoryProvider.GetRepositories().ToArray();

        using var sourceCacheContext = new SourceCacheContext();
        var nugetLogger = new NuGetNullLogger();
        var cancellationToken = CancellationToken.None;
        var dependencyContext = DependencyContext.Default
                                ?? throw new Exception("No DependencyContext set for application");

        var packageDependencyTree = await GetPackageDependencyTreeAsync(
            packageIdentity,
            NuGetFramework.Parse(dotNetFrameworkVersion.GetTargetFrameworkMoniker()),
            repositories,
            dependencyContext,
            sourceCacheContext,
            nugetLogger,
            cancellationToken
        );

        var packagesToInstall = GetPackagesToInstallAsync(
            packageDependencyTree,
            sourceRepositoryProvider,
            nugetLogger);

        await InstallPackagesAsync(
            packageIdentity,
            packagesToInstall,
            sourceCacheContext,
            nugetLogger,
            cancellationToken);
    }

    public Task<PackageInstallInfo?> GetPackageInstallInfoAsync(string packageId, string packageVersion)
    {
        PackageInstallInfo? installInfo = null;

        var installPath = GetInstallPath(new NugetPackageIdentity(packageId, NuGetVersion.Parse(packageVersion)));
        if (installPath != null) installInfo = GetInstallInfo(installPath);

        return Task.FromResult(installInfo);
    }

    public async Task<CachedPackage[]> GetCachedPackagesAsync(bool loadMetadata = false)
    {
        var nuGetCacheDir = new DirectoryInfo(GetNuGetCacheDirectoryPath());

        return await GetCachedPackagesAsync(
            nuGetCacheDir.GetFiles(PackageInstallInfoFileName, SearchOption.AllDirectories),
            loadMetadata);
    }

    public async Task<CachedPackage[]> GetExplicitlyInstalledCachedPackagesAsync(bool loadMetadata = false)
    {
        var nuGetCacheDir = new DirectoryInfo(GetNuGetCacheDirectoryPath());

        var packageInstallInfoFiles = nuGetCacheDir
            .GetFiles(PackageInstallInfoFileName, SearchOption.AllDirectories)
            .Where(f => GetInstallInfo(f)?.InstallReason == PackageInstallReason.Explicit);

        return await GetCachedPackagesAsync(packageInstallInfoFiles, loadMetadata);
    }

    public Task<HashSet<PackageAsset>> GetCachedPackageAssetsAsync(
        string packageId,
        string packageVersion,
        DotNetFrameworkVersion dotNetFrameworkVersion)
    {
        var packageIdentity = new NugetPackageIdentity(packageId, new NuGetVersion(packageVersion));

        var installPath = GetInstallPath(packageIdentity);
        if (installPath == null)
            throw new Exception($"Package {packageIdentity} is not installed.");

        var nugetFramework = NuGetFramework.Parse(dotNetFrameworkVersion.GetTargetFrameworkMoniker());
        var frameworkReducer = new FrameworkReducer();
        using var packageReader = new PackageFolderReader(installPath);

        var libItems = GetNearestItems(packageReader.GetLibItems().ToArray())
            .Where(i => i.EndsWithIgnoreCase(".dll"));

        var runtimeItems = GetRuntimeItems(installPath, nugetFramework);

        // Put runtime items first as they are more specific, and later when we deploy these assets, if multiple
        // asset files with the same are being deployed, only the first one is deployed, the rest are ignored.
        // We want the runtime version of an assembly to be deployed, not the generic "lib" assembly.
        var final = runtimeItems
            .Concat(libItems)
            .Where(IsLib)
            .Select(itemPath => new PackageAsset(itemPath))
            .ToHashSet();

        return Task.FromResult(final);

        IEnumerable<string> GetNearestItems(FrameworkSpecificGroup[] items)
        {
            var nearestLibItemsFramework = frameworkReducer
                .GetNearest(nugetFramework, items.Select(x => x.TargetFramework));
            return items
                .Where(x => x.TargetFramework.Equals(nearestLibItemsFramework))
                .SelectMany(x => x.Items)
                .Select(i => Path.Combine(installPath, i));
        }
    }

    public async Task<HashSet<PackageAsset>> GetRecursivePackageAssetsAsync(
        string packageId,
        string packageVersion,
        DotNetFrameworkVersion dotNetFrameworkVersion)
    {
        var packageIdentity = new NugetPackageIdentity(packageId, new NuGetVersion(packageVersion));

        if (!IsInstalled(packageIdentity))
        {
            await InstallPackageAsync(packageId, packageVersion, dotNetFrameworkVersion);
        }

        var nugetFramework = NuGetFramework.Parse(dotNetFrameworkVersion.GetTargetFrameworkMoniker());
        using var sourceCacheContext = new SourceCacheContext();
        var nugetLogger = new NuGetNullLogger();
        var cancellationToken = CancellationToken.None;
        var dependencyContext = DependencyContext.Default
                                ?? throw new Exception("No DependencyContext set for application");

        var packageDependencyTree = await GetPackageDependencyTreeAsync(
            packageIdentity,
            nugetFramework,
            GetSourceRepositoryProvider().GetRepositories().ToArray(),
            dependencyContext,
            sourceCacheContext,
            nugetLogger,
            cancellationToken
        );

        var allPackages = packageDependencyTree.GetAllPackages();
        var assets = new HashSet<PackageAsset>();

        foreach (var package in allPackages)
        {
            if (!IsInstalled(package))
            {
                await InstallPackageAsync(package.Id, package.Version.ToString(), dotNetFrameworkVersion);
            }

            var packageAssets = await GetCachedPackageAssetsAsync(
                package.Id,
                package.Version.ToString(),
                dotNetFrameworkVersion);

            foreach (var asset in packageAssets)
            {
                assets.Add(asset);
            }
        }

        return assets;
    }


    public Task DeleteCachedPackageAsync(string packageId, string packageVersion)
    {
        var packageIdentity = new NugetPackageIdentity(packageId, new NuGetVersion(packageVersion));

        var installPath = GetInstallPath(packageIdentity);

        if (installPath != null && Directory.Exists(installPath))
        {
            Directory.Delete(installPath, true);
        }

        return Task.CompletedTask;
    }

    public Task PurgePackageCacheAsync()
    {
        var nuGetCacheDir = new DirectoryInfo(GetNuGetCacheDirectoryPath());

        foreach (var directory in nuGetCacheDir.GetDirectories())
        {
            directory.Delete(true);
        }

        return Task.CompletedTask;
    }


    private async Task<CachedPackage[]> GetCachedPackagesAsync(
        IEnumerable<FileInfo> packageInstallInfoFiles,
        bool loadMetadata = false)
    {
        var cachedPackages = new List<CachedPackage>();

        foreach (var infoFile in packageInstallInfoFiles)
        {
            var packageDir = infoFile.Directory!;

            try
            {
                using var packageReader = new PackageFolderReader(packageDir);
                var nuspecReader = packageReader.NuspecReader;
                var installInfo = GetInstallInfo(infoFile)!;

                string packageId = nuspecReader.GetId();
                string title = nuspecReader.GetTitle().DefaultIfNullOrWhitespace(nuspecReader.GetId());

                string icon = nuspecReader.GetIconUrl();
                if (string.IsNullOrWhiteSpace(icon) && !string.IsNullOrWhiteSpace(nuspecReader.GetIcon()))
                {
                    try
                    {
                        var iconPath = Path.Combine(packageDir.FullName, nuspecReader.GetIcon());

                        if (File.Exists(iconPath))
                        {
                            var iconBytes = await File.ReadAllBytesAsync(iconPath);
                            icon =
                                $"data:image/{Path.GetExtension(iconPath).Trim('.')};base64,{Convert.ToBase64String(iconBytes)}";
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Could not load icon at: {PackageDir}", packageDir.FullName);
                    }
                }

                var cachedPackage = new CachedPackage(
                    packageId,
                    title,
                    installInfo.InstallReason,
                    packageDir.FullName
                )
                {
                    Version = nuspecReader.GetVersion().ToString(),
                    Authors = nuspecReader.GetAuthors(),
                    Description = nuspecReader.GetDescription(),
                    IconUrl = StringUtil.ToUriOrDefault(icon),
                    ProjectUrl = StringUtil.ToUriOrDefault(nuspecReader.GetProjectUrl()),
                    PackageDetailsUrl = null, // Does not exist in nuspec file
                    LicenseUrl = StringUtil.ToUriOrDefault(nuspecReader.GetLicenseUrl()),
                    ReadmeUrl = StringUtil.ToUriOrDefault(nuspecReader.GetReadme()),
                    ReportAbuseUrl = null,
                    RequireLicenseAcceptance = nuspecReader.GetRequireLicenseAcceptance(),
                    Dependencies = nuspecReader.GetDependencyGroups()
                        .Select(ds => new PackageDependencySet(
                            ds.TargetFramework.ToString() ?? string.Empty,
                            ds.Packages.Select(p => $"{p.Id} {p.VersionRange}").ToArray()))
                        .ToArray(),
                    DownloadCount = null,
                    PublishedDate = null
                };

                cachedPackages.Add(cachedPackage);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Could not gather info about package located in: {PackageDir}",
                    packageDir.FullName);
            }
        }

        if (loadMetadata)
        {
            try
            {
                await HydrateMetadataAsync(cachedPackages, TimeSpan.FromSeconds(cachedPackages.Count * 5));
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error hydrating metadata");
            }
        }

        return cachedPackages.ToArray();
    }

    private async Task<PackageDependencyTree> GetPackageDependencyTreeAsync(
        NugetPackageIdentity package,
        NuGetFramework framework,
        SourceRepository[] repositories,
        DependencyContext hostDependencies,
        SourceCacheContext cacheContext,
        INugetLogger nugetLogger,
        CancellationToken cancellationToken)
    {
        var packageDependencyTree = new PackageDependencyTree(package);

        foreach (var repository in repositories)
        {
            // Get the dependency info for the package.
            SourcePackageDependencyInfo? dependencyInfo;

            try
            {
                var dependencyInfoResource =
                    await repository.GetResourceAsync<DependencyInfoResource>(cancellationToken);
                dependencyInfo = await dependencyInfoResource.ResolvePackage(
                    package,
                    framework,
                    cacheContext,
                    nugetLogger,
                    cancellationToken);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error getting dependency info of package: {Id} {Version}", package.Id,
                    package.Version);
                continue;
            }

            // No info for the package in this repository.
            if (dependencyInfo == null)
            {
                continue;
            }

            // Filter the dependency info. Don't bring in any dependencies that are provided by the host.
            var actualDependencyInfo = new SourcePackageDependencyInfo(
                dependencyInfo.Id,
                dependencyInfo.Version,
                dependencyInfo.Dependencies.Where(dep => !DependencySuppliedByHost(hostDependencies, dep)),
                dependencyInfo.Listed,
                dependencyInfo.Source);

            packageDependencyTree.DependencyInfo = actualDependencyInfo;

            // Recurse through each dependency package.
            foreach (var dep in actualDependencyInfo.Dependencies)
            {
                var depDependencyTree = await GetPackageDependencyTreeAsync(
                    new NugetPackageIdentity(dep.Id, dep.VersionRange.MinVersion),
                    framework,
                    repositories,
                    hostDependencies,
                    cacheContext,
                    nugetLogger,
                    cancellationToken);

                packageDependencyTree.Dependencies.Add(depDependencyTree);
            }

            break;
        }

        return packageDependencyTree;
    }

    private IEnumerable<SourcePackageDependencyInfo> GetPackagesToInstallAsync(
        PackageDependencyTree packageDependencyTree,
        SourceRepositoryProvider sourceRepositoryProvider,
        INugetLogger nugetLogger)
    {
        var allPackages = packageDependencyTree.GetAllPackages();

        // Create a package resolver context (this is used to help figure out which actual package versions to install).
        var resolverContext = new PackageResolverContext(
            DependencyBehavior.Lowest,
            [packageDependencyTree.Identity.Id],
            [],
            [],
            [],
            allPackages,
            sourceRepositoryProvider.GetRepositories().Select(s => s.PackageSource),
            nugetLogger);

        var resolver = new PackageResolver();

        // Work out the actual set of packages to install.
        var packagesToInstall = resolver
            .Resolve(resolverContext, CancellationToken.None)
            .Select(p => allPackages.Single(x => PackageIdentityComparer.Default.Equals(x, p)));

        return packagesToInstall;
    }

    private bool IsInstalled(NugetPackageIdentity packageIdentity) => GetInstallPath(packageIdentity) != null;

    private async Task InstallPackagesAsync(
        NugetPackageIdentity explicitPackageToInstallIdentity,
        IEnumerable<SourcePackageDependencyInfo> packagesToInstall,
        SourceCacheContext sourceCacheContext,
        INugetLogger nugetLogger,
        CancellationToken cancellationToken)
    {
        await appStatusMessagePublisher.PublishAsync(
            $"Installing package {explicitPackageToInstallIdentity.Id} (v.{explicitPackageToInstallIdentity.Version.ToString()})...");

        foreach (var packageToInstall in packagesToInstall)
        {
            bool isExplicitPackageToInstall = packageToInstall.Id == explicitPackageToInstallIdentity.Id &&
                                              packageToInstall.Version == explicitPackageToInstallIdentity.Version;

            var installPath = GetInstallPath(packageToInstall);

            try
            {
                if (installPath == null)
                {
                    var downloadResource =
                        await packageToInstall.Source.GetResourceAsync<DownloadResource>(cancellationToken);

                    // Download the package.
                    _ = await downloadResource.GetDownloadResourceResultAsync(
                        packageToInstall,
                        new PackageDownloadContext(sourceCacheContext),
                        GetNuGetCacheDirectoryPath(),
                        nugetLogger,
                        cancellationToken);

                    /*
                     * Removed extracting of package since there is no way to control it to extract to the same directory
                     * the downloader resource above extracts to.
                     *
                     * The download resource will download the package in dir "GetNuGetCacheDirectoryPath()/packageID/version/"
                     * The PackageExtractor will extract the package in dir "GetNuGetCacheDirectoryPath()/packageID.version/"
                     */

                    // Extract the package into the target directory.
                    // var packageExtractionContext = new PackageExtractionContext(
                    //     PackageSaveMode.Defaultv3,
                    //     XmlDocFileSaveMode.None,
                    //     ClientPolicyContext.GetClientPolicy(nuGetSettings, logger),
                    //     logger);
                    //
                    // await PackageExtractor.ExtractPackageAsync(
                    //     downloadResult.PackageSource,
                    //     downloadResult.PackageStream,
                    //     packagePathResolver,
                    //     packageExtractionContext,
                    //     cancellationToken);

                    installPath = GetInstallPath(packageToInstall);

                    if (installPath == null)
                    {
                        throw new Exception(
                            $"Could not locate install path after package was installed. Package ID: {packageToInstall.Id}, Version: {packageToInstall.Version}");
                    }
                }

                // Package is already installed, but could be installed by other tools
                var installInfo = GetInstallInfo(installPath);
                if (installInfo == null)
                {
                    installInfo = isExplicitPackageToInstall
                        ? new PackageInstallInfo(
                            packageToInstall.Id,
                            packageToInstall.Version.ToString(),
                            PackageInstallReason.Explicit)
                        : new PackageInstallInfo(
                            packageToInstall.Id,
                            packageToInstall.Version.ToString(),
                            PackageInstallReason.Dependency);

                    SaveInstallInfo(installPath, installInfo);
                }
                else if (installInfo.InstallReason == PackageInstallReason.Dependency && isExplicitPackageToInstall)
                {
                    installInfo.ChangeInstallReason(PackageInstallReason.Explicit);
                    SaveInstallInfo(installPath, installInfo);
                }
            }
            catch
            {
                await appStatusMessagePublisher.PublishAsync(
                    $"Error installing package {explicitPackageToInstallIdentity.Id} (v.{explicitPackageToInstallIdentity.Version.ToString()})");
                throw;
            }
        }

        await appStatusMessagePublisher.PublishAsync(
            $"Installed package {explicitPackageToInstallIdentity.Id} (v.{explicitPackageToInstallIdentity.Version.ToString()})");
    }

    public async Task<Dictionary<PackageIdentity, PackageMetadata?>> GetExtendedMetadataAsync(
        IEnumerable<PackageIdentity> packageIdentities,
        CancellationToken cancellationToken = default)
    {
        var metadatas = packageIdentities
            .ToDictionary(p => p, _ => (PackageMetadata?)null);

        using var sourceCacheContext = new SourceCacheContext();
        var sourceRepositories = GetSourceRepositoryProvider().GetRepositories();

        foreach (var sourceRepository in sourceRepositories)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            var targets = metadatas
                .Where(x => x.Value == null)
                .Select(x => x.Key)
                .ToArray();

            await Parallel.ForEachAsync(targets, cancellationToken, async (packageIdentity, ct) =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                IPackageSearchMetadata? metadata;

                try
                {
                    var resource = await sourceRepository.GetResourceAsync<PackageMetadataResource>(ct);
                    metadata = await resource.GetMetadataAsync(
                        new NugetPackageIdentity(packageIdentity.Id, new NuGetVersion(packageIdentity.Version)),
                        sourceCacheContext,
                        NuGetNullLogger.Instance,
                        ct);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error getting package metadata");
                    return;
                }

                if (metadata == null)
                {
                    return;
                }

                var package = new PackageMetadata(packageIdentity.Id, string.Empty);

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                await MapAsync(metadata, package);

                package.Version ??= packageIdentity.Version;

                metadatas[packageIdentity] = package;
            });
        }

        return metadatas;
    }

    private async Task HydrateMetadataAsync(IEnumerable<PackageMetadata> packages, TimeSpan? timeout = null)
    {
        var needsProcessing = packages.Where(p => p.HasMissingMetadata()).ToList();
        if (!needsProcessing.Any())
            return;

        using var sourceCacheContext = new SourceCacheContext();
        var sourceRepositories = GetSourceRepositoryProvider().GetRepositories();
        using var cancellationTokenSource =
            timeout == null ? new CancellationTokenSource() : new CancellationTokenSource(timeout.Value);

        foreach (var sourceRepository in sourceRepositories)
        {
            if (!needsProcessing.Any() || cancellationTokenSource.IsCancellationRequested)
            {
                break;
            }

            var found = new List<PackageMetadata>();

            foreach (var package in needsProcessing)
            {
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }

                if (package.Version == null)
                {
                    continue;
                }

                IPackageSearchMetadata? metadata;

                try
                {
                    var resource =
                        await sourceRepository.GetResourceAsync<PackageMetadataResource>(cancellationTokenSource.Token);
                    metadata = await resource.GetMetadataAsync(
                        new NugetPackageIdentity(package.PackageId, new NuGetVersion(package.Version)),
                        sourceCacheContext,
                        NuGetNullLogger.Instance,
                        cancellationTokenSource.Token);
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Error hydrating metadata");
                    continue;
                }

                if (metadata == null)
                {
                    continue;
                }

                await MapAsync(metadata, package);
                found.Add(package);
            }

            foreach (var cachedPackage in found)
            {
                needsProcessing.Remove(cachedPackage);
            }
        }
    }

    private IEnumerable<string> GetRuntimeItems(string packageDirectory, NuGetFramework framework)
    {
        var runtimesDirectory = new DirectoryInfo(Path.Combine(packageDirectory, "runtimes"));

        if (!runtimesDirectory.Exists)
        {
            return [];
        }

        var platformRids = GetCurrentPlatformRIDs();

        var ridDirs = runtimesDirectory.GetDirectories()
            .Where(d => platformRids.Contains(d.Name))
            .OrderBy(d => Array.IndexOf(platformRids, d.Name));

        var interestingRuntimeSubDirs = new HashSet<string>(["native", "nativeassets", "lib"]);

        foreach (var ridDir in ridDirs)
        {
            var nativeOrLibDirs = ridDir.GetDirectories().Where(d => interestingRuntimeSubDirs.Contains(d.Name));

            foreach (var nativeOrLibDir in nativeOrLibDirs)
            {
                var frameworkDirs = nativeOrLibDir.GetDirectories();

                var compatibleFrameworkDirs = frameworkDirs
                    .Where(d =>
                    {
                        var fw = NuGetFramework.Parse(d.Name);
                        return !fw.IsUnsupported && DefaultCompatibilityProvider.Instance.IsCompatible(framework, fw);
                    })
                    .Select(d => new
                    {
                        Dir = d,
                        Framework = NuGetFramework.Parse(d.Name)
                    })
                    .ToList();

                var fwReducer = new FrameworkReducer();
                while (compatibleFrameworkDirs.Any())
                {
                    var nearest = fwReducer.GetNearest(framework, compatibleFrameworkDirs.Select(x => x.Framework));

                    if (nearest == null)
                    {
                        break;
                    }

                    var frameworkDir = compatibleFrameworkDirs.First(x => x.Framework == nearest);

                    var files = frameworkDir.Dir.GetFiles();
                    if (files.Any(f => IsLib(f.FullName)))
                    {
                        return files.Where(f => f.Name != "_._").Select(x => x.FullName);
                    }

                    compatibleFrameworkDirs.Remove(frameworkDir);
                }

                var directFiles = nativeOrLibDir.GetFiles();

                if (directFiles.Any(f => IsLib(f.FullName)))
                {
                    return directFiles.Where(f => f.Name != "_._").Select(x => x.FullName);
                }
            }
        }

        return [];
    }

    private static bool IsLib(string filePath)
    {
        var fileName = Path.GetFileName(filePath);

        return fileName.EndsWithIgnoreCase(".dll")
               || fileName.EndsWithIgnoreCase(".so")
               || fileName.EndsWithIgnoreCase(".dylib");
    }

    private static string[] GetCurrentPlatformRIDs()
    {
        var rids = new List<string>();

        var platform = PlatformUtil.GetOSPlatform();
        var arch = RuntimeInformation.OSArchitecture;

        // RIDs are in increasing specificity (last item in RID array is most specific)
        if (platform == OSPlatform.Windows)
        {
            if (arch == Architecture.X64)
            {
                rids.AddRange([
                    "amd64", "win", "x64", "win-x64", "win7", "win7-x64", "win8", "win8-x64", "win81", "win81-x64",
                    "win10", "win10-x64"
                ]);
            }
            else if (arch == Architecture.X86)
            {
                rids.AddRange([
                    "win", "x86", "win-x86", " win7", " win7-x86", " win8", " win8-x86", " win81", " win81-x86",
                    " win10", " win10-x86"
                ]);
            }
            else if (arch == Architecture.Arm64)
            {
                rids.AddRange([
                    "win", "arm64", "win-arm64", "win7", "win7-arm64", "win8", "win8-arm64", "win81", "win81-arm64",
                    "win10", "win10-arm64"
                ]);
            }
            else if (arch == Architecture.Arm)
            {
                rids.AddRange([
                    "win", "arm", "win-arm", "win7", "win7-arm", "win8", "win8-arm", "win81", "win81-arm", "win10",
                    "win10-arm"
                ]);
            }

            var osVersion = Environment.OSVersion.Version;

            // If older than Windows 10
            if (osVersion.Major < 10)
            {
                rids = rids.Where(r => !r.StartsWith("win10")).ToList();

                // If older than Windows 8.1
                if (osVersion.Major < 6 || osVersion is { Major: 6, Minor: < 3 })
                {
                    rids.RemoveAll(r => r.StartsWith("win81"));
                }

                // If older than Windows 8
                if (osVersion.Major < 6 || osVersion is { Major: 6, Minor: < 2 })
                {
                    rids.RemoveAll(r => r.StartsWith("win8"));
                }
            }
        }
        else if (platform == OSPlatform.OSX)
        {
            if (arch == Architecture.X64)
            {
                rids.AddRange(["unix", "osx", "osx-x64"]);
                if (OperatingSystem.IsMacCatalyst()) rids.Add("maccatalyst-x64");
            }
            else if (arch == Architecture.Arm64)
            {
                rids.AddRange(["unix", "osx", "osx-x64", "osx-arm64"]);
                if (OperatingSystem.IsMacCatalyst()) rids.Add("maccatalyst-arm64");
            }
        }
        else
        {
            if (arch == Architecture.X64)
            {
                rids.AddRange(["unix", "linux", "linux-x64", "linux-musl-x64"]);
            }
            else if (arch == Architecture.X86)
            {
                rids.AddRange(["unix", "linux", "linux-x86"]);
            }
            else if (arch == Architecture.Arm64)
            {
                rids.AddRange(["unix", "linux", "linux-arm64", "linux-musl-arm64"]);
            }
            else if (arch == Architecture.Arm)
            {
                rids.AddRange(["unix", "linux", "linux-arm", "linux-musl-arm"]);
            }
            else if (arch == Architecture.S390x)
            {
                rids.AddRange(["unix", "linux", "linux-s390x"]);
            }

            if (!RuntimeInformation.OSDescription.ContainsIgnoreCase("musl"))
            {
                rids.RemoveAll(r => r.Contains("-musl-"));
            }
        }

        // Add current system RID as most specific so far
        rids.Add(RuntimeInformation.RuntimeIdentifier);

        // Add WASM as most specific
        if (arch == Architecture.Wasm)
        {
            rids.AddRange(["wasm", "browser-wasm"]);
        }

        return rids
            .Where(rid => !string.IsNullOrWhiteSpace(rid))
            .Distinct()
            .Reverse()
            .ToArray();
    }

    private bool DependencySuppliedByHost(DependencyContext hostDependencies, PackageDependency dep)
    {
        if (RuntimeProvidedPackages.IsPackageProvidedByRuntime(dep.Id))
        {
            return true;
        }

        return false;

        // // Check if there is a runtime library with the same ID as the package is available in the host's runtime libraries.
        // var runtimeLibs = hostDependencies.RuntimeLibraries.Where(r => r.Name == dep.Id);
        //
        // return runtimeLibs.Any(r =>
        // {
        //     // What version of the library is the host using?
        //     var parsedLibVersion = NuGetVersion.Parse(r.Version);
        //
        //     if (parsedLibVersion.IsPrerelease)
        //     {
        //         // Always use pre-release versions from the host, otherwise it becomes
        //         // a nightmare to develop across multiple active versions.
        //         return true;
        //     }
        //     else
        //     {
        //         // Does the host version satisfy the version range of the requested package?
        //         // If so, we can provide it; otherwise, we cannot.
        //         return dep.VersionRange.Satisfies(parsedLibVersion);
        //     }
        // });
    }

    private SourceRepositoryProvider GetSourceRepositoryProvider()
    {
        var nugetSettings = global::NuGet.Configuration.Settings.LoadDefaultSettings(
            global::NuGet.Configuration.Settings.DefaultSettingsFileName
        );

        var sourceProvider = new PackageSourceProvider(nugetSettings, [
            new PackageSource(NugetApiUri)
        ]);

        return new SourceRepositoryProvider(sourceProvider, Repository.Provider.GetCoreV3());
    }

    private string GetNuGetCacheDirectoryPath()
    {
        var path = Path.Combine(settings.PackageCacheDirectoryPath, "NuGet");
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
        packageMetadata.PackageDetailsUrl = searchMetadata.PackageDetailsUrl;
        packageMetadata.LicenseUrl = searchMetadata.LicenseUrl;
        packageMetadata.ReadmeUrl = searchMetadata.ReadmeUrl;
        packageMetadata.ReportAbuseUrl = searchMetadata.ReportAbuseUrl;
        packageMetadata.DownloadCount = searchMetadata.DownloadCount;
        packageMetadata.PublishedDate = searchMetadata.Published?.UtcDateTime;
        packageMetadata.RequireLicenseAcceptance = searchMetadata.RequireLicenseAcceptance;

        packageMetadata.Dependencies = searchMetadata.DependencySets
            .Select(ds => new PackageDependencySet(
                ds.TargetFramework.ToString() ?? string.Empty,
                ds.Packages.Select(p => $"{p.Id} {p.VersionRange}").ToArray()))
            .ToArray();

        var versions = await searchMetadata.GetVersionsAsync().ConfigureAwait(false);
        var latestVersion = versions?.MaxBy(v => v.Version);

        packageMetadata.Version ??= latestVersion?.Version.ToString();

        packageMetadata.LatestAvailableVersion = latestVersion?.Version.ToString() ?? (await GetPackageVersionsAsync(
            packageMetadata.PackageId,
            searchMetadata.Identity.Version.IsPrerelease)).MaxBy(NuGetVersion.Parse);
    }

    private string? GetInstallPath(NugetPackageIdentity packageIdentity)
    {
        bool TryGetPath(string? path, out string? newPath)
        {
            newPath = null;

            if (path == null)
                return false;

            if (Directory.Exists(path))
            {
                newPath = path;
                return true;
            }

            // When revision is 0 (x.x.x.0), directory on disk will most likely have been saved without
            // the revision number in the directory name (ie. x.x.x)
            if (packageIdentity.Version.Revision == 0)
            {
                path = path[..^2];
                if (Directory.Exists(path))
                {
                    newPath = path;
                    return true;
                }
            }

            return false;
        }

        string dirPath = Path.Combine(
            GetNuGetCacheDirectoryPath(),
            packageIdentity.Id.ToLowerInvariant(),
            packageIdentity.Version.ToString().ToLowerInvariant());

        if (TryGetPath(dirPath, out string? installPath))
            return installPath;

        return null;
    }

    private PackageInstallInfo? GetInstallInfo(string installPath)
    {
        var infoFile = Path.Combine(installPath, PackageInstallInfoFileName);
        return !File.Exists(infoFile)
            ? null
            : JsonSerializer.Deserialize<PackageInstallInfo>(File.ReadAllText(infoFile));
    }

    private PackageInstallInfo? GetInstallInfo(FileInfo installInfoFile)
    {
        return !installInfoFile.Exists
            ? null
            : JsonSerializer.Deserialize<PackageInstallInfo>(File.ReadAllText(installInfoFile.FullName));
    }

    private void SaveInstallInfo(string installPath, PackageInstallInfo info)
    {
        var json = JsonSerializer.Serialize(info, true);
        File.WriteAllText(Path.Combine(installPath, PackageInstallInfoFileName), json);
    }
}
