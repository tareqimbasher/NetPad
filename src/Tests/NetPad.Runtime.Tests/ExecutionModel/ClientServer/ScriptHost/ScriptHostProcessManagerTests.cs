using System.Text.Json.Nodes;
using NetPad.DotNet;
using NetPad.ExecutionModel.ClientServer.ScriptHost;

namespace NetPad.Runtime.Tests.ExecutionModel.ClientServer.ScriptHost;

public class ScriptHostProcessManagerTests
{
    private const string NetCoreApp = "Microsoft.NETCore.App";
    private const string AspNetCoreApp = "Microsoft.AspNetCore.App";
    private const string DotNetRoot = "/usr/share/dotnet";

    [Fact]
    public void RetargetRuntimeConfig_SetsTfmToTargetMajorVersion()
    {
        var runtimeConfig = ScriptHostRuntimeConfig();

        ScriptHostProcessManager.RetargetRuntimeConfig(runtimeConfig, 9, [], DotNetRoot);

        Assert.Equal("net9.0", runtimeConfig["runtimeOptions"]!["tfm"]!.GetValue<string>());
    }

    [Fact]
    public void RetargetRuntimeConfig_UsesMajorVersion_WhenAReleaseRuntimeIsInstalled()
    {
        var runtimeConfig = ScriptHostRuntimeConfig();
        var runtimes = Runtimes((NetCoreApp, "10.0.13"), (AspNetCoreApp, "10.0.4"), (AspNetCoreApp, "10.0.13"));

        ScriptHostProcessManager.RetargetRuntimeConfig(runtimeConfig, 10, runtimes, DotNetRoot);

        Assert.Equal("10.0.0", FrameworkVersion(runtimeConfig, NetCoreApp));
        Assert.Equal("10.0.0", FrameworkVersion(runtimeConfig, AspNetCoreApp));
    }

    [Fact]
    public void RetargetRuntimeConfig_UsesMajorVersion_WhenReleaseAndPrereleaseRuntimesAreInstalled()
    {
        var runtimeConfig = ScriptHostRuntimeConfig();
        var runtimes = Runtimes(
            (NetCoreApp, "11.0.0-rc.3.26522.148"),
            (NetCoreApp, "11.0.0"),
            (AspNetCoreApp, "11.0.0"),
            (AspNetCoreApp, "11.0.0-rc.3.26522.148"));

        ScriptHostProcessManager.RetargetRuntimeConfig(runtimeConfig, 11, runtimes, DotNetRoot);

        Assert.Equal("11.0.0", FrameworkVersion(runtimeConfig, NetCoreApp));
        Assert.Equal("11.0.0", FrameworkVersion(runtimeConfig, AspNetCoreApp));
    }

    [Fact]
    public void RetargetRuntimeConfig_UsesNewestPrereleaseVersion_WhenOnlyPrereleaseRuntimesAreInstalled()
    {
        var runtimeConfig = ScriptHostRuntimeConfig();
        var runtimes = Runtimes(
            (NetCoreApp, "10.0.0"),
            (NetCoreApp, "11.0.0-rc.2.26473.101"),
            (NetCoreApp, "11.0.0-rc.3.26522.148"),
            (AspNetCoreApp, "11.0.0-rc.3.26522.148"));

        ScriptHostProcessManager.RetargetRuntimeConfig(runtimeConfig, 11, runtimes, DotNetRoot);

        Assert.Equal("11.0.0-rc.3.26522.148", FrameworkVersion(runtimeConfig, NetCoreApp));
        Assert.Equal("11.0.0-rc.3.26522.148", FrameworkVersion(runtimeConfig, AspNetCoreApp));
    }

    [Fact]
    public void RetargetRuntimeConfig_DecidesForEachFrameworkSeparately()
    {
        var runtimeConfig = ScriptHostRuntimeConfig();
        var runtimes = Runtimes((NetCoreApp, "11.0.0"), (AspNetCoreApp, "11.0.0-rc.3.26522.148"));

        ScriptHostProcessManager.RetargetRuntimeConfig(runtimeConfig, 11, runtimes, DotNetRoot);

        Assert.Equal("11.0.0", FrameworkVersion(runtimeConfig, NetCoreApp));
        Assert.Equal("11.0.0-rc.3.26522.148", FrameworkVersion(runtimeConfig, AspNetCoreApp));
    }

    [Fact]
    public void RetargetRuntimeConfig_UsesMajorVersion_WhenNoRuntimeOfThatMajorVersionIsInstalled()
    {
        var runtimeConfig = ScriptHostRuntimeConfig();
        var runtimes = Runtimes((NetCoreApp, "10.0.0"), (AspNetCoreApp, "10.0.0"));

        ScriptHostProcessManager.RetargetRuntimeConfig(runtimeConfig, 11, runtimes, DotNetRoot);

        Assert.Equal("11.0.0", FrameworkVersion(runtimeConfig, NetCoreApp));
        Assert.Equal("11.0.0", FrameworkVersion(runtimeConfig, AspNetCoreApp));
    }

    [Fact]
    public void RetargetRuntimeConfig_IgnoresRuntimesInOtherDotNetRoots()
    {
        var runtimeConfig = ScriptHostRuntimeConfig();
        DotNetRuntimeVersion[] runtimes =
        [
            new(NetCoreApp, new SemanticVersion("11.0.0"), "/opt/dotnet"),
            new(NetCoreApp, new SemanticVersion("11.0.0-rc.3.26522.148"), DotNetRoot),
        ];

        ScriptHostProcessManager.RetargetRuntimeConfig(runtimeConfig, 11, runtimes, DotNetRoot);

        Assert.Equal("11.0.0-rc.3.26522.148", FrameworkVersion(runtimeConfig, NetCoreApp));
    }

    [Fact]
    public void RetargetRuntimeConfig_ConsidersEveryRoot_WhenNoDotNetRootIsGiven()
    {
        var runtimeConfig = ScriptHostRuntimeConfig();
        DotNetRuntimeVersion[] runtimes =
        [
            new(NetCoreApp, new SemanticVersion("11.0.0"), "/opt/dotnet"),
            new(NetCoreApp, new SemanticVersion("11.0.0-rc.3.26522.148"), DotNetRoot),
        ];

        ScriptHostProcessManager.RetargetRuntimeConfig(runtimeConfig, 11, runtimes, null);

        Assert.Equal("11.0.0", FrameworkVersion(runtimeConfig, NetCoreApp));
    }

    private static JsonNode ScriptHostRuntimeConfig() => JsonNode.Parse(
        """
        {
          "runtimeOptions": {
            "tfm": "net6.0",
            "frameworks": [
              { "name": "Microsoft.NETCore.App", "version": "6.0.0" },
              { "name": "Microsoft.AspNetCore.App", "version": "6.0.0" }
            ]
          }
        }
        """)!;

    private static string FrameworkVersion(JsonNode runtimeConfig, string frameworkName) =>
        runtimeConfig["runtimeOptions"]!["frameworks"]!.AsArray()
            .Single(f => f!["name"]!.GetValue<string>() == frameworkName)!["version"]!
            .GetValue<string>();

    private static DotNetRuntimeVersion[] Runtimes(params (string FrameworkName, string Version)[] runtimes) =>
        runtimes.Select(r => new DotNetRuntimeVersion(r.FrameworkName, new SemanticVersion(r.Version), DotNetRoot))
            .ToArray();
}
