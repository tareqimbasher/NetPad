using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NetPad.Application;
using NetPad.Apps.CQs;
using NetPad.DotNet;

namespace NetPad.Apps.Common.Tests.CQs;

public class CheckAppDependenciesQueryHandlerTests
{
    private const string DotNetRoot = "/usr/share/dotnet";

    private readonly Mock<IDotNetInfo> _dotNetInfo = new();

    public CheckAppDependenciesQueryHandlerTests()
    {
        _dotNetInfo.Setup(i => i.GetCurrentDotNetRuntimeVersion()).Returns(new SemanticVersion("10.0.1"));
        _dotNetInfo
            .Setup(i => i.LocateDotNetRootDirectoryForFramework(It.IsAny<DotNetFrameworkVersion>()))
            .Returns(DotNetRoot);
    }

    [Fact]
    public async Task ReportsSupportedSdkVersionsThatHaveNoAspNetRuntime()
    {
        InstallSdks("3.1.426", "8.0.416", "9.0.304", "10.0.402");
        InstallRuntimes(AspNetRuntime("3.1.32"), AspNetRuntime("9.0.11"));

        var result = await Handle();

        Assert.Equal([DotNetFrameworkVersion.DotNet8, DotNetFrameworkVersion.DotNet10],
            result.FrameworkVersionsMissingAspNetRuntime);
        Assert.True(result.CanRunScripts);
    }

    [Fact]
    public async Task CannotRunScripts_WhenNoInstalledSdkVersionHasAnAspNetRuntime()
    {
        InstallSdks("8.0.416", "9.0.304");
        InstallRuntimes(new DotNetRuntimeVersion("Microsoft.NETCore.App", new SemanticVersion("9.0.11"), DotNetRoot));

        var result = await Handle();

        Assert.Equal([DotNetFrameworkVersion.DotNet8, DotNetFrameworkVersion.DotNet9],
            result.FrameworkVersionsMissingAspNetRuntime);
        Assert.False(result.CanRunScripts);
    }

    [Fact]
    public async Task ReportsNothingMissing_WhenRuntimesCannotBeListed()
    {
        InstallSdks("9.0.304");
        _dotNetInfo.Setup(i => i.GetDotNetRuntimeVersions()).Throws(new Exception("dotnet --list-runtimes failed"));

        var result = await Handle();

        Assert.Empty(result.FrameworkVersionsMissingAspNetRuntime);
        Assert.True(result.CanRunScripts);
    }

    private async Task<AppDependencyCheckResult> Handle()
    {
        var handler = new CheckAppDependenciesQuery.Handler(
            _dotNetInfo.Object,
            NullLogger<CheckAppDependenciesQuery.Handler>.Instance);

        return await handler.Handle(new CheckAppDependenciesQuery(), CancellationToken.None);
    }

    private void InstallSdks(params string[] versions)
    {
        _dotNetInfo
            .Setup(i => i.GetDotNetSdkVersions())
            .Returns(versions.Select(v => new DotNetSdkVersion(new SemanticVersion(v), DotNetRoot)).ToArray());
    }

    private void InstallRuntimes(params DotNetRuntimeVersion[] runtimes)
    {
        _dotNetInfo.Setup(i => i.GetDotNetRuntimeVersions()).Returns(runtimes);
    }

    private static DotNetRuntimeVersion AspNetRuntime(string version) =>
        new("Microsoft.AspNetCore.App", new SemanticVersion(version), DotNetRoot);
}
