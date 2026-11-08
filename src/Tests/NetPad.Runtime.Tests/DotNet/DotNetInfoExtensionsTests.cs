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
    public void LocateLatestSdkForFramework_ReturnsNewestSdkOfThatVersion(
        DotNetFrameworkVersion frameworkVersion,
        string expectedSdkVersion)
    {
        var dotNetInfo = DotNetInfoWithInstalledSdks("8.0.302", "8.0.416", "9.0.100", "9.0.304", "10.0.402");

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
