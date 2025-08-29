using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Application;
using NetPad.Configuration;
using NetPad.DotNet;
using NetPad.Packages;
using NetPad.Packages.NuGet;
using NetPad.Tests;
using NetPad.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace NetPad.Runtime.Tests.Packages.NuGet;

public class NuGetPackageProviderTests : TestBase, IAsyncLifetime
{
    private readonly string _root;
    private readonly string _packageCacheDir;

    public NuGetPackageProviderTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _root = TempFileHelper.CreateTempTestDirectory<NuGetPackageProviderTests>();
        _packageCacheDir = Path.Combine(_root, "pkgcache");
        Directory.CreateDirectory(_packageCacheDir);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }

        return Task.CompletedTask;
    }

    [Fact]
    public async Task SearchPackages_Dont_Load_Metadata()
    {
        var provider = CreatePackageProvider();
        var take = 3;

        var result = await provider.SearchPackagesAsync("json", 0, take, false);

        Assert.True(result.Length >= take);
        Assert.Null(result[0].PublishedDate);
    }

    [Fact]
    public async Task SearchPackages_And_Load_Metadata()
    {
        var provider = CreatePackageProvider();
        var take = 3;

        var result = await provider.SearchPackagesAsync("json", 0, take, false, true);

        Assert.True(result.Length >= take);
        Assert.NotNull(result[0].PublishedDate);
    }

    [Fact]
    public async Task GetPackageVersions_Do_Not_Include_Prerelease()
    {
        var provider = CreatePackageProvider();

        var versions = await provider.GetPackageVersionsAsync("Newtonsoft.Json", false);

        Assert.Contains("13.0.1", versions);
        Assert.DoesNotContain("13.0.1-beta1", versions);
    }

    [Fact]
    public async Task GetPackageVersions_Include_Prerelease()
    {
        var provider = CreatePackageProvider();

        var versions = await provider.GetPackageVersionsAsync("Newtonsoft.Json", true);

        Assert.Contains("13.0.1", versions);
        Assert.Contains("13.0.1-beta1", versions);
    }

    [Fact]
    public async Task GetExtendedMetadata()
    {
        var provider = CreatePackageProvider();
        var newtonsoft = new PackageIdentity("Newtonsoft.Json", "13.0.1");
        var serilog = new PackageIdentity("Serilog", "4.3.0");

        var metadatas = await provider.GetExtendedMetadataAsync(
            [
                newtonsoft,
                serilog
            ]
        );

        Assert.Equal(2, metadatas.Count);
        Assert.True(metadatas.ContainsKey(newtonsoft));
        Assert.NotNull(metadatas[newtonsoft]!.Description);
        Assert.True(metadatas.ContainsKey(serilog));
        Assert.NotNull(metadatas[serilog]!.Description);
    }

    [Fact]
    public async Task InstallPackage()
    {
        var provider = CreatePackageProvider();

        await provider.InstallPackageAsync("Newtonsoft.Json", "13.0.1", DotNetFrameworkVersion.DotNet8);

        var installInfo = await provider.GetPackageInstallInfoAsync("Newtonsoft.Json", "13.0.1");

        Assert.NotNull(installInfo);
        Assert.Equal("Newtonsoft.Json", installInfo.PackageId);
        Assert.Equal("13.0.1", installInfo.Version);
    }

    [Fact]
    public async Task InstallPackage_Installs_Package_To_Configured_Package_Directory()
    {
        var provider = CreatePackageProvider();

        await provider.InstallPackageAsync("Newtonsoft.Json", "13.0.1", DotNetFrameworkVersion.DotNet8);

        var assets = await provider.GetCachedPackageAssetsAsync(
            "Newtonsoft.Json",
            "13.0.1",
            DotNetFrameworkVersion.DotNet8);

        Assert.NotEmpty(assets);
        Assert.StartsWith(_packageCacheDir, assets.First().Path);
    }

    [Fact]
    public async Task GetCachedPackages_None_By_Default()
    {
        var provider = CreatePackageProvider();

        var cachedPackages = await provider.GetCachedPackagesAsync();

        Assert.Empty(cachedPackages);
    }

    [Theory]
    [InlineData("Newtonsoft.Json", "13.0.1", 1)] // Has no dependencies
    [InlineData("Serilog.Sinks.File", "7.0.0", 2)] // Will also install "Serilog" dependency
    public async Task GetCachedPackages_Returns_All_Cached_Packages_Regardless_Of_Install_Reason(
        string packageId,
        string packageVersion,
        int expectedCachedPackageCount
    )
    {
        var provider = CreatePackageProvider();
        await provider.InstallPackageAsync(packageId, packageVersion, DotNetFrameworkVersion.DotNet8);

        var cachedPackages = await provider.GetCachedPackagesAsync();

        Assert.Equal(expectedCachedPackageCount, cachedPackages.Length);
        Assert.Equal(1, cachedPackages.Count(x => x.PackageId == packageId && x.Version == packageVersion));
    }

    [Fact]
    public async Task GetPackageInstallInfo_Returns_Correct_Info_For_Explictly_Installed_Packages()
    {
        var provider = CreatePackageProvider();
        await provider.InstallPackageAsync("Newtonsoft.Json", "13.0.1", DotNetFrameworkVersion.DotNet8);

        var installInfo = await provider.GetPackageInstallInfoAsync("Newtonsoft.Json", "13.0.1");

        Assert.NotNull(installInfo);
        Assert.Equal("Newtonsoft.Json", installInfo.PackageId);
        Assert.Equal("13.0.1", installInfo.Version);
        Assert.Equal(PackageInstallReason.Explicit, installInfo.InstallReason);
    }

    [Fact]
    public async Task GetPackageInstallInfo_Returns_Correct_Info_For_NonExplictly_Installed_Packages()
    {
        var provider = CreatePackageProvider();
        // Will also install "Serilog v4.2.0" package as dependency
        await provider.InstallPackageAsync("Serilog.Sinks.File", "7.0.0", DotNetFrameworkVersion.DotNet8);

        var installInfo = await provider.GetPackageInstallInfoAsync("Serilog", "4.2.0");

        Assert.NotNull(installInfo);
        Assert.Equal("Serilog", installInfo.PackageId);
        Assert.Equal("4.2.0", installInfo.Version);
        Assert.Equal(PackageInstallReason.Dependency, installInfo.InstallReason);
    }

    [Theory]
    [InlineData("Newtonsoft.Json", "13.0.1")] // Has no dependencies
    [InlineData("Serilog.Sinks.File", "7.0.0")] // Will also install "Serilog" dependency
    public async Task GetExplicitlyInstalledCachedPackages_Returns_Explicitly_Installed_Packages_Only(
        string packageId,
        string packageVersion
    )
    {
        var provider = CreatePackageProvider();
        await provider.InstallPackageAsync(packageId, packageVersion, DotNetFrameworkVersion.DotNet8);

        var explictlyInstalledPackages = await provider.GetExplicitlyInstalledCachedPackagesAsync();

        Assert.Single(explictlyInstalledPackages);
        var package = explictlyInstalledPackages.Single();
        Assert.Equal(packageId, package.PackageId);
        Assert.Equal(packageVersion, package.Version);
    }


    public static IEnumerable<object?[]> GetCachedPackageAssetsData =>
    [
        ["Newtonsoft.Json", "13.0.1", new[] { "Newtonsoft.Json.dll" }],
        ["Serilog.Sinks.File", "7.0.0", new[] { "Serilog.Sinks.File.dll" }],
        ["Serilog.Sinks.Seq", "9.0.0", new[] { "Serilog.Sinks.Seq.dll" }],
        // Resolves a runtime-specific binary asset
        [
            "sqlitepclraw.lib.e_sqlite3", "2.1.7", new[]
            {
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? "e_sqlite3.dll"
                    : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                        ? "libe_sqlite3.so"
                        : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                            ? "libe_sqlite3.dylib"
                            : throw new NotSupportedException(
                                $"Test case does not support: {RuntimeInformation.OSDescription}")
            }
        ],
    ];

    [Theory]
    [MemberData(nameof(GetCachedPackageAssetsData))]
    public async Task GetCachedPackageAssets_Returns_Package_Assets_Only(
        string packageId,
        string packageVersion,
        string[] expectedAssetNames
    )
    {
        var provider = CreatePackageProvider();
        await provider.InstallPackageAsync(packageId, packageVersion, DotNetFrameworkVersion.DotNet8);

        var assets = await provider.GetCachedPackageAssetsAsync(
            packageId,
            packageVersion,
            DotNetFrameworkVersion.DotNet8);

        var assetNames = assets.Select(x => Path.GetFileName(x.Path)).ToArray();

        Assert.Equal(expectedAssetNames, assetNames);
    }

    public static IEnumerable<object?[]> GetRecursivePackageAssetsData =>
    [
        ["Newtonsoft.Json", "13.0.1", new[] { "Newtonsoft.Json.dll" }],
        ["Serilog.Sinks.File", "7.0.0", new[] { "Serilog.Sinks.File.dll", "Serilog.dll" }],
        ["Serilog.Sinks.Seq", "9.0.0", new[] { "Serilog.Sinks.Seq.dll", "Serilog.dll", "Serilog.Sinks.File.dll" }],
        [
            "SQLitePCLRaw.bundle_green", "2.1.7",
            new[]
            {
                "SQLitePCLRaw.batteries_v2.dll",
                "SQLitePCLRaw.provider.e_sqlite3.dll",
                "SQLitePCLRaw.core.dll",
                // Binary asset
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                    ? "e_sqlite3.dll"
                    : RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                        ? "libe_sqlite3.so"
                        : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                            ? "libe_sqlite3.dylib"
                            : throw new NotSupportedException(
                                $"Test case does not support: {RuntimeInformation.OSDescription}")
            }
        ],
    ];

    [Theory]
    [MemberData(nameof(GetRecursivePackageAssetsData))]
    public async Task GetRecursivePackageAssets_Returns_Package_And_Dependency_Assets(
        string packageId,
        string packageVersion,
        string[] expectedAssetNames
    )
    {
        var provider = CreatePackageProvider();
        await provider.InstallPackageAsync(packageId, packageVersion, DotNetFrameworkVersion.DotNet8);

        var assets = await provider.GetRecursivePackageAssetsAsync(
            packageId,
            packageVersion,
            DotNetFrameworkVersion.DotNet8);

        // Use ToHashSet at the end because different versions of the same assembly can come back
        // in the depedency tree of some packages. For example: "Serilog.Sinks.Seq" will include
        // two versions of the "Serilog.dll" assembly.
        var assetNames = assets.Select(x => Path.GetFileName(x.Path)).ToHashSet();

        Assert.Equal(expectedAssetNames, assetNames);
    }

    [Fact]
    public async Task DeleteCachedPackage_Removes_Package_From_Cache()
    {
        var provider = CreatePackageProvider();
        await provider.InstallPackageAsync("Newtonsoft.Json", "13.0.1", DotNetFrameworkVersion.DotNet8);

        var installInfo = await provider.GetPackageInstallInfoAsync("Newtonsoft.Json", "13.0.1");

        // Sanity check
        Assert.NotNull(installInfo);
        Assert.Equal("Newtonsoft.Json", installInfo.PackageId);
        Assert.Equal("13.0.1", installInfo.Version);

        await provider.DeleteCachedPackageAsync("Newtonsoft.Json", "13.0.1");

        installInfo = await provider.GetPackageInstallInfoAsync("Newtonsoft.Json", "13.0.1");
        Assert.Null(installInfo);
    }

    [Fact]
    public async Task PurgePackageCache_Removes_All_Cached_Packages()
    {
        var provider = CreatePackageProvider();
        await provider.InstallPackageAsync("Newtonsoft.Json", "13.0.1", DotNetFrameworkVersion.DotNet8);
        await provider.InstallPackageAsync("Serilog", "4.3.0", DotNetFrameworkVersion.DotNet8);

        await provider.PurgePackageCacheAsync();
        var cachedPackages = await provider.GetCachedPackagesAsync();

        Assert.Empty(cachedPackages);
    }

    private NuGetPackageProvider CreatePackageProvider() => new NuGetPackageProvider(
        new Settings(null!, _packageCacheDir),
        ServiceProvider.GetRequiredService<IAppStatusMessagePublisher>(),
        ServiceProvider.GetRequiredService<ILogger<NuGetPackageProvider>>()
    );
}
