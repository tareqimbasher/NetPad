using System.Text.Json;
using Moq;
using NetPad.DotNet;

namespace NetPad.Runtime.Tests.DotNet;

public partial class DotNetCSharpProjectTests
{
    [Fact]
    public async Task CreateAsync_PinsGlobalJsonToTheSdkOfTheTargetFramework()
    {
        var proj = NewProjectWithInstalledSdks("8.0.302", "8.0.416", "10.0.402");

        await proj.CreateAsync(DotNetFrameworkVersion.DotNet8, ProjectOutputType.Library);

        Assert.Equal(("8.0.416", "latestFeature"), ReadGlobalJsonSdk());
    }

    [Fact]
    public async Task CreateAsync_PinsGlobalJsonToTheExactPreviewSdk_WhenOnlyAPreviewIsInstalled()
    {
        var proj = NewProjectWithInstalledSdks("8.0.416", "9.0.100-preview.6.24328.19");

        await proj.CreateAsync(DotNetFrameworkVersion.DotNet9, ProjectOutputType.Library);

        Assert.Equal(("9.0.100-preview.6.24328.19", "latestFeature"), ReadGlobalJsonSdk());
    }

    [Fact]
    public async Task CreateAsync_PinsGlobalJsonToTheMajorVersion_WhenNoSdkOfTheTargetFrameworkIsFound()
    {
        var proj = NewProjectWithInstalledSdks("10.0.402");

        await proj.CreateAsync(DotNetFrameworkVersion.DotNet8, ProjectOutputType.Library);

        Assert.Equal(("8.0.100", "latestFeature"), ReadGlobalJsonSdk());
    }

    [Fact]
    public async Task UpdateTargetFrameworkAsync_PinsGlobalJsonToTheSdkOfTheNewTargetFramework()
    {
        var proj = NewProjectWithInstalledSdks("8.0.416", "10.0.402");
        await proj.CreateAsync(DotNetFrameworkVersion.DotNet10, ProjectOutputType.Library);

        await proj.UpdateTargetFrameworkAsync(DotNetFrameworkVersion.DotNet8);

        Assert.Equal(("8.0.416", "latestFeature"), ReadGlobalJsonSdk());
    }

    private DotNetCSharpProject NewProjectWithInstalledSdks(params string[] versions)
    {
        var dotNetRoot = Path.Combine(_root, "dotnet");
        foreach (var version in versions)
        {
            Directory.CreateDirectory(Path.Combine(dotNetRoot, "sdk", version));
        }

        var dotNetInfo = new Mock<IDotNetInfo>();
        dotNetInfo
            .Setup(i => i.GetDotNetSdkVersions())
            .Returns(versions.Select(v => new DotNetSdkVersion(new SemanticVersion(v), dotNetRoot)).ToArray());

        return new DotNetCSharpProject(dotNetInfo.Object, _projectDir, "project", _packageCacheDir);
    }

    private (string? Version, string? RollForward) ReadGlobalJsonSdk()
    {
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(_projectDir, "global.json")));
        var sdk = json.RootElement.GetProperty("sdk");
        return (sdk.GetProperty("version").GetString(), sdk.GetProperty("rollForward").GetString());
    }
}
