using Moq;
using NetPad.DotNet;

namespace NetPad.Runtime.Tests.DotNet;

public class DotNetInfoExtensionsTests : IAsyncLifetime
{
    private readonly string _dotNetRoot = TempFileHelper.CreateTempTestDirectory<DotNetInfoExtensionsTests>();

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync()
    {
        Directory.Delete(_dotNetRoot, recursive: true);
        return Task.CompletedTask;
    }

    [Theory]
    [InlineData(DotNetFrameworkVersion.DotNet8, "8.0.416")]
    [InlineData(DotNetFrameworkVersion.DotNet9, "9.0.304")]
    [InlineData(DotNetFrameworkVersion.DotNet10, "10.0.402")]
    [InlineData(DotNetFrameworkVersion.DotNet11, "11.0.100")]
    public void LocateLatestSdkForFramework_ReturnsNewestSdkOfThatVersion(
        DotNetFrameworkVersion frameworkVersion,
        string expectedSdkVersion)
    {
        var dotNetInfo = DotNetInfoWithInstalledSdks(
            "8.0.302", "8.0.416", "9.0.100", "9.0.304", "10.0.402", "11.0.100-rc.3.26522.148", "11.0.100");

        var sdk = dotNetInfo.LocateLatestSdkForFramework(frameworkVersion);

        Assert.NotNull(sdk);
        Assert.Equal(expectedSdkVersion, sdk.Version.ToString());
        Assert.Equal(_dotNetRoot, sdk.DotNetRootDirectory);
    }

    [Fact]
    public void LocateLatestSdkForFramework_ReturnsPreviewSdk_WhenItIsTheOnlyOneOfThatVersion()
    {
        var dotNetInfo = DotNetInfoWithInstalledSdks("8.0.416", "9.0.100-preview.6.24328.19");

        var sdk = dotNetInfo.LocateLatestSdkForFramework(DotNetFrameworkVersion.DotNet9);

        Assert.NotNull(sdk);
        Assert.Equal("9.0.100-preview.6.24328.19", sdk.Version.ToString());
    }

    [Fact]
    public void LocateLatestSdkForFramework_ReturnsNull_WhenNoSdkOfThatVersionIsInstalled()
    {
        var dotNetInfo = DotNetInfoWithInstalledSdks("8.0.416", "10.0.402");

        Assert.Null(dotNetInfo.LocateLatestSdkForFramework(DotNetFrameworkVersion.DotNet9));
    }

    [Fact]
    public void LocateLatestSdkForFramework_ReturnsNull_WhenNewestSdkWasRemovedAfterSdksWereListed()
    {
        var dotNetInfo = DotNetInfoWithInstalledSdks("8.0.401", "8.0.416");
        Directory.Delete(Path.Combine(_dotNetRoot, "sdk", "8.0.416"));

        Assert.Null(dotNetInfo.LocateLatestSdkForFramework(DotNetFrameworkVersion.DotNet8));
    }

    [Fact]
    public void GetSupportedFrameworkVersions_ReturnsEachSupportedFrameworkVersionOnce()
    {
        var dotNetInfo = DotNetInfoWithInstalledSdks("8.0.302", "8.0.416", "10.0.402");

        var frameworkVersions = dotNetInfo.GetSupportedFrameworkVersions();

        Assert.Equal([DotNetFrameworkVersion.DotNet8, DotNetFrameworkVersion.DotNet10], frameworkVersions.Order());
    }

    [Fact]
    public void GetSupportedFrameworkVersions_IgnoresSdksThatAreTooOldOrTooNew()
    {
        var dotNetInfo = DotNetInfoWithInstalledSdks(
            "3.1.426",
            "8.0.416",
            $"{DotNetFrameworkVersionUtil.MaxSupportedDotNetVersion + 1}.0.100");

        Assert.Equal([DotNetFrameworkVersion.DotNet8], dotNetInfo.GetSupportedFrameworkVersions());
    }

    [Fact]
    public void IsAspNetRuntimeInstalledFor_ReturnsTrue_WhenRuntimeOfThatVersionIsInstalledInTheSdkRoot()
    {
        var dotNetInfo = DotNetInfoWithInstalledRuntimes(
            _dotNetRoot,
            new DotNetRuntimeVersion("Microsoft.NETCore.App", new SemanticVersion("9.0.11"), _dotNetRoot),
            new DotNetRuntimeVersion("Microsoft.AspNetCore.App", new SemanticVersion("9.0.11"), _dotNetRoot));

        Assert.True(dotNetInfo.IsAspNetRuntimeInstalledFor(DotNetFrameworkVersion.DotNet9));
    }

    [Fact]
    public void IsAspNetRuntimeInstalledFor_IgnoresFrameworkNameCase()
    {
        var dotNetInfo = DotNetInfoWithInstalledRuntimes(
            _dotNetRoot,
            new DotNetRuntimeVersion("microsoft.aspnetcore.app", new SemanticVersion("9.0.11"), _dotNetRoot));

        Assert.True(dotNetInfo.IsAspNetRuntimeInstalledFor(DotNetFrameworkVersion.DotNet9));
    }

    [Fact]
    public void IsAspNetRuntimeInstalledFor_ReturnsFalse_WhenOnlyOtherVersionsOrOtherRuntimesAreInstalled()
    {
        var dotNetInfo = DotNetInfoWithInstalledRuntimes(
            _dotNetRoot,
            new DotNetRuntimeVersion("Microsoft.NETCore.App", new SemanticVersion("9.0.11"), _dotNetRoot),
            new DotNetRuntimeVersion("Microsoft.AspNetCore.App", new SemanticVersion("8.0.22"), _dotNetRoot),
            new DotNetRuntimeVersion("Microsoft.AspNetCore.App", new SemanticVersion("10.0.1"), _dotNetRoot));

        Assert.False(dotNetInfo.IsAspNetRuntimeInstalledFor(DotNetFrameworkVersion.DotNet9));
    }

    [Fact]
    public void IsAspNetRuntimeInstalledFor_ReturnsTrue_WhenOnlyAPrereleaseRuntimeOfThatVersionIsInstalled()
    {
        var dotNetInfo = DotNetInfoWithInstalledRuntimes(
            _dotNetRoot,
            new DotNetRuntimeVersion("Microsoft.AspNetCore.App", new SemanticVersion("11.0.0-rc.3.26522.148"), _dotNetRoot));

        Assert.True(dotNetInfo.IsAspNetRuntimeInstalledFor(DotNetFrameworkVersion.DotNet11));
    }

    [Fact]
    public void IsAspNetRuntimeInstalledFor_ReturnsFalse_WhenRuntimeIsInADifferentRootThanTheSdk()
    {
        var dotNetInfo = DotNetInfoWithInstalledRuntimes(
            _dotNetRoot,
            new DotNetRuntimeVersion("Microsoft.AspNetCore.App", new SemanticVersion("9.0.11"), "/other/dotnet"));

        Assert.False(dotNetInfo.IsAspNetRuntimeInstalledFor(DotNetFrameworkVersion.DotNet9));
    }

    [Fact]
    public void IsAspNetRuntimeInstalledFor_CountsRuntimeWithUnknownRoot()
    {
        var dotNetInfo = DotNetInfoWithInstalledRuntimes(
            _dotNetRoot,
            new DotNetRuntimeVersion("Microsoft.AspNetCore.App", new SemanticVersion("9.0.11")));

        Assert.True(dotNetInfo.IsAspNetRuntimeInstalledFor(DotNetFrameworkVersion.DotNet9));
    }

    [Fact]
    public void IsAspNetRuntimeInstalledFor_CountsRuntimeInAnyRoot_WhenNoSdkRootIsFoundForThatVersion()
    {
        var dotNetInfo = DotNetInfoWithInstalledRuntimes(
            null,
            new DotNetRuntimeVersion("Microsoft.AspNetCore.App", new SemanticVersion("9.0.11"), "/other/dotnet"));

        Assert.True(dotNetInfo.IsAspNetRuntimeInstalledFor(DotNetFrameworkVersion.DotNet9));
    }

    [Fact]
    public void IsRuntimeInstalled_ReturnsTrue_WhenRuntimeOfTheSameMajorVersionIsInTheRoot()
    {
        var dotNetInfo = DotNetInfoWithInstalledRuntimes(
            _dotNetRoot,
            new DotNetRuntimeVersion("Microsoft.NETCore.App", new SemanticVersion("10.0.1"), _dotNetRoot));

        Assert.True(dotNetInfo.IsRuntimeInstalled(DotNetRuntimeVersion.NetCoreFrameworkName, 10, _dotNetRoot));
    }

    [Fact]
    public void IsRuntimeInstalled_CountsANewerMajorVersion_OnlyWhenAllowed()
    {
        var dotNetInfo = DotNetInfoWithInstalledRuntimes(
            _dotNetRoot,
            new DotNetRuntimeVersion("Microsoft.NETCore.App", new SemanticVersion("11.0.0"), _dotNetRoot));

        Assert.True(dotNetInfo.IsRuntimeInstalled(
            DotNetRuntimeVersion.NetCoreFrameworkName, 10, _dotNetRoot, allowNewerMajorVersion: true));
        Assert.False(dotNetInfo.IsRuntimeInstalled(
            DotNetRuntimeVersion.NetCoreFrameworkName, 10, _dotNetRoot, allowNewerMajorVersion: false));
    }

    [Fact]
    public void IsRuntimeInstalled_ReturnsFalse_WhenOnlyAnOlderMajorVersionIsInstalled()
    {
        var dotNetInfo = DotNetInfoWithInstalledRuntimes(
            _dotNetRoot,
            new DotNetRuntimeVersion("Microsoft.NETCore.App", new SemanticVersion("8.0.22"), _dotNetRoot));

        Assert.False(dotNetInfo.IsRuntimeInstalled(
            DotNetRuntimeVersion.NetCoreFrameworkName, 10, _dotNetRoot, allowNewerMajorVersion: true));
    }

    [Fact]
    public void IsRuntimeInstalled_ReturnsFalse_WhenRuntimeIsInADifferentRoot()
    {
        var dotNetInfo = DotNetInfoWithInstalledRuntimes(
            _dotNetRoot,
            new DotNetRuntimeVersion("Microsoft.NETCore.App", new SemanticVersion("10.0.1"), "/other/dotnet"));

        Assert.False(dotNetInfo.IsRuntimeInstalled(DotNetRuntimeVersion.NetCoreFrameworkName, 10, _dotNetRoot));
    }

    [Fact]
    public void IsRuntimeInstalled_CountsRuntimeWithUnknownRoot()
    {
        var dotNetInfo = DotNetInfoWithInstalledRuntimes(
            _dotNetRoot,
            new DotNetRuntimeVersion("Microsoft.NETCore.App", new SemanticVersion("10.0.1")));

        Assert.True(dotNetInfo.IsRuntimeInstalled(DotNetRuntimeVersion.NetCoreFrameworkName, 10, _dotNetRoot));
    }

    [Fact]
    public void IsRuntimeInstalled_CountsRuntimeInAnyRoot_WhenNoRootIsGiven()
    {
        var dotNetInfo = DotNetInfoWithInstalledRuntimes(
            _dotNetRoot,
            new DotNetRuntimeVersion("Microsoft.NETCore.App", new SemanticVersion("10.0.1"), "/other/dotnet"));

        Assert.True(dotNetInfo.IsRuntimeInstalled(DotNetRuntimeVersion.NetCoreFrameworkName, 10, dotNetRoot: null));
    }

    [Fact]
    public void IsRuntimeInstalled_DoesNotCountARuntimeOfAnotherFramework()
    {
        var dotNetInfo = DotNetInfoWithInstalledRuntimes(
            _dotNetRoot,
            new DotNetRuntimeVersion("Microsoft.AspNetCore.App", new SemanticVersion("10.0.1"), _dotNetRoot));

        Assert.False(dotNetInfo.IsRuntimeInstalled(
            DotNetRuntimeVersion.NetCoreFrameworkName, 10, _dotNetRoot, allowNewerMajorVersion: true));
    }

    private static IDotNetInfo DotNetInfoWithInstalledRuntimes(string? sdkRoot, params DotNetRuntimeVersion[] runtimes)
    {
        var dotNetInfo = new Mock<IDotNetInfo>();
        dotNetInfo
            .Setup(i => i.LocateDotNetRootDirectoryForFramework(It.IsAny<DotNetFrameworkVersion>()))
            .Returns(sdkRoot);
        dotNetInfo
            .Setup(i => i.GetDotNetRuntimeVersions())
            .Returns(runtimes);

        return dotNetInfo.Object;
    }

    private IDotNetInfo DotNetInfoWithInstalledSdks(params string[] versions)
    {
        foreach (var version in versions)
        {
            Directory.CreateDirectory(Path.Combine(_dotNetRoot, "sdk", version));
        }

        var dotNetInfo = new Mock<IDotNetInfo>();
        dotNetInfo
            .Setup(i => i.GetDotNetSdkVersions())
            .Returns(versions.Select(v => new DotNetSdkVersion(new SemanticVersion(v), _dotNetRoot)).ToArray());

        return dotNetInfo.Object;
    }
}
