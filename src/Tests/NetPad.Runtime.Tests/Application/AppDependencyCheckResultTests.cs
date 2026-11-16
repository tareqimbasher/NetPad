using NetPad.Application;
using NetPad.DotNet;

namespace NetPad.Runtime.Tests.Application;

public class AppDependencyCheckResultTests
{
    [Fact]
    public void CanRunScripts_IsTrue_WhenNoSupportedSdkVersionIsMissingTheAspNetRuntime()
    {
        var result = Result(["8.0.416", "9.0.304"], missingAspNetRuntime: []);

        Assert.True(result.CanRunScripts);
    }

    [Fact]
    public void CanRunScripts_IsTrue_WhenOnlySomeSupportedSdkVersionsAreMissingTheAspNetRuntime()
    {
        var result = Result(["8.0.416", "9.0.304"], missingAspNetRuntime: [DotNetFrameworkVersion.DotNet8]);

        Assert.True(result.CanRunScripts);
    }

    [Fact]
    public void CanRunScripts_IsFalse_WhenEverySupportedSdkVersionIsMissingTheAspNetRuntime()
    {
        var result = Result(
            ["8.0.416", "8.0.302", "9.0.304"],
            missingAspNetRuntime: [DotNetFrameworkVersion.DotNet8, DotNetFrameworkVersion.DotNet9]);

        Assert.False(result.CanRunScripts);
    }

    [Fact]
    public void CanRunScripts_IsFalse_WhenNoSupportedSdkIsInstalled()
    {
        var result = Result(["3.1.426"], missingAspNetRuntime: []);

        Assert.False(result.CanRunScripts);
    }

    [Fact]
    public void CanRunScripts_IsFalse_WhenNoSdkIsInstalled()
    {
        var result = Result([], missingAspNetRuntime: []);

        Assert.False(result.CanRunScripts);
    }

    private static AppDependencyCheckResult Result(string[] sdkVersions, DotNetFrameworkVersion[] missingAspNetRuntime)
    {
        return new AppDependencyCheckResult(
            "10.0.1",
            sdkVersions.Select(v => new DotNetSdkVersion(new SemanticVersion(v), "/dotnet")).ToArray(),
            missingAspNetRuntime);
    }
}
