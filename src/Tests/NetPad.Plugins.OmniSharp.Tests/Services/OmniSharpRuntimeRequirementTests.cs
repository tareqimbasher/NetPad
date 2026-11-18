using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NetPad.DotNet;
using NetPad.Plugins.OmniSharp.Services;

namespace NetPad.Plugins.OmniSharp.Tests.Services;

public sealed class OmniSharpRuntimeRequirementTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory().FullName;

    private string ExecutablePath => Path.Combine(_directory, "OmniSharp");

    [Fact]
    public void Read_AllowsNewerMajorVersion_WhenRollForwardIsLatestMajor()
    {
        WriteRuntimeConfig("""
            {
              "runtimeOptions": {
                "tfm": "net10.0",
                "rollForward": "LatestMajor",
                "framework": { "name": "Microsoft.NETCore.App", "version": "10.0.0" }
              }
            }
            """);

        var requirement = OmniSharpRuntimeRequirement.Read(ExecutablePath, NullLogger.Instance);

        Assert.Equal(new OmniSharpRuntimeRequirement(10, AllowsNewerMajorVersion: true), requirement);
    }

    [Fact]
    public void Read_AllowsNewerMajorVersion_WhenRollForwardIsMajor()
    {
        WriteRuntimeConfig("""
            {
              "runtimeOptions": {
                "rollForward": "major",
                "framework": { "name": "Microsoft.NETCore.App", "version": "10.0.0" }
              }
            }
            """);

        var requirement = OmniSharpRuntimeRequirement.Read(ExecutablePath, NullLogger.Instance);

        Assert.Equal(new OmniSharpRuntimeRequirement(10, AllowsNewerMajorVersion: true), requirement);
    }

    [Fact]
    public void Read_RequiresTheSameMajorVersion_WhenNoRollForwardIsSet()
    {
        WriteRuntimeConfig("""
            {
              "runtimeOptions": {
                "framework": { "name": "Microsoft.NETCore.App", "version": "10.0.0" }
              }
            }
            """);

        var requirement = OmniSharpRuntimeRequirement.Read(ExecutablePath, NullLogger.Instance);

        Assert.Equal(new OmniSharpRuntimeRequirement(10, AllowsNewerMajorVersion: false), requirement);
    }

    [Fact]
    public void Read_RequiresTheSameMajorVersion_WhenRollForwardOnlyAllowsNewerMinorVersions()
    {
        WriteRuntimeConfig("""
            {
              "runtimeOptions": {
                "rollForward": "LatestMinor",
                "framework": { "name": "Microsoft.NETCore.App", "version": "10.0.0" }
              }
            }
            """);

        var requirement = OmniSharpRuntimeRequirement.Read(ExecutablePath, NullLogger.Instance);

        Assert.Equal(new OmniSharpRuntimeRequirement(10, AllowsNewerMajorVersion: false), requirement);
    }

    [Fact]
    public void Read_TakesTheBaseRuntime_FromTheFrameworksArray()
    {
        WriteRuntimeConfig("""
            {
              "runtimeOptions": {
                "rollForward": "LatestMajor",
                "frameworks": [
                  { "name": "Microsoft.AspNetCore.App", "version": "11.0.0" },
                  { "name": "Microsoft.NETCore.App", "version": "10.0.0" }
                ]
              }
            }
            """);

        var requirement = OmniSharpRuntimeRequirement.Read(ExecutablePath, NullLogger.Instance);

        Assert.Equal(new OmniSharpRuntimeRequirement(10, AllowsNewerMajorVersion: true), requirement);
    }

    [Fact]
    public void Read_TakesTheMajorVersion_OfAPrereleaseRuntime()
    {
        WriteRuntimeConfig("""
            {
              "runtimeOptions": {
                "rollForward": "LatestMajor",
                "framework": { "name": "Microsoft.NETCore.App", "version": "6.0.0-preview.7.21317.1" }
              }
            }
            """);

        var requirement = OmniSharpRuntimeRequirement.Read(ExecutablePath, NullLogger.Instance);

        Assert.Equal(new OmniSharpRuntimeRequirement(6, AllowsNewerMajorVersion: true), requirement);
    }

    [Fact]
    public void Read_ReadsTheFileNamedAfterTheExecutable_WhenTheExecutableHasAnExtension()
    {
        File.WriteAllText(Path.Combine(_directory, "OmniSharp.runtimeconfig.json"), """
            { "runtimeOptions": { "framework": { "name": "Microsoft.NETCore.App", "version": "10.0.0" } } }
            """);

        var requirement = OmniSharpRuntimeRequirement.Read(Path.Combine(_directory, "OmniSharp.exe"), NullLogger.Instance);

        Assert.Equal(10, requirement?.MajorVersion);
    }

    [Fact]
    public void Read_ReturnsNull_WhenTheFileIsMissing()
    {
        Assert.Null(OmniSharpRuntimeRequirement.Read(ExecutablePath, NullLogger.Instance));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{ "runtimeOptions": { "framework": { "name": "Microsoft.NETCore.App", "version": 10 } } }""")]
    [InlineData("""{ "runtimeOptions": { "framework": { "name": "Microsoft.NETCore.App", "version": "ten" } } }""")]
    [InlineData("""{ "runtimeOptions": [] }""")]
    public void Read_ReturnsNull_WhenTheFileIsMalformed(string contents)
    {
        WriteRuntimeConfig(contents);

        Assert.Null(OmniSharpRuntimeRequirement.Read(ExecutablePath, NullLogger.Instance));
    }

    [Fact]
    public void Read_ReturnsNull_WhenTheBuildIsSelfContained()
    {
        WriteRuntimeConfig("""
            {
              "runtimeOptions": {
                "tfm": "net10.0",
                "includedFrameworks": [ { "name": "Microsoft.NETCore.App", "version": "10.0.1" } ]
              }
            }
            """);

        Assert.Null(OmniSharpRuntimeRequirement.Read(ExecutablePath, NullLogger.Instance));
    }

    [Fact]
    public void IsMet_ChecksForTheBaseRuntimeInTheGivenRoot()
    {
        var dotNetInfo = new Mock<IDotNetInfo>();
        dotNetInfo.Setup(i => i.GetDotNetRuntimeVersions()).Returns([
            new DotNetRuntimeVersion(DotNetRuntimeVersion.NetCoreFrameworkName, new SemanticVersion("11.0.0"), "/a"),
            new DotNetRuntimeVersion(DotNetRuntimeVersion.AspNetCoreFrameworkName, new SemanticVersion("10.0.1"), "/b")
        ]);
        var requirement = new OmniSharpRuntimeRequirement(10, AllowsNewerMajorVersion: true);

        Assert.True(requirement.IsMet(dotNetInfo.Object, "/a"));
        Assert.False(requirement.IsMet(dotNetInfo.Object, "/b"));
    }

    [Theory]
    [InlineData(true, ".NET 10 runtime or newer")]
    [InlineData(false, ".NET 10 runtime")]
    public void ToString_NamesTheRuntimeToInstall(bool allowsNewerMajorVersion, string expected)
    {
        Assert.Equal(expected, new OmniSharpRuntimeRequirement(10, allowsNewerMajorVersion).ToString());
    }

    private void WriteRuntimeConfig(string contents) =>
        File.WriteAllText(Path.Combine(_directory, "OmniSharp.runtimeconfig.json"), contents);

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
