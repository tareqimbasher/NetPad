using NetPad.DotNet;

namespace NetPad.Runtime.Tests.DotNet;

public class DotNetInfoTests
{
    [Fact]
    public void RuntimeVersions_KeepTheSameRuntimeOncePerRoot_WhenTwoRootsListIt()
    {
        var output = ListRuntimesOutput(
            "Microsoft.AspNetCore.App 10.0.1 [/missing/shared/Microsoft.AspNetCore.App]",
            "Microsoft.NETCore.App 10.0.1 [/missing/shared/Microsoft.NETCore.App]");

        var runtimes = DotNetInfo.DistinctRuntimeVersions(
            DotNetInfo.ParseRuntimeVersions(output, "/usr/lib/dotnet")
                .Concat(DotNetInfo.ParseRuntimeVersions(output, "/home/user/.dotnet")));

        Assert.Equal(
            [
                new DotNetRuntimeVersion("Microsoft.AspNetCore.App", new SemanticVersion("10.0.1"), "/home/user/.dotnet"),
                new DotNetRuntimeVersion("Microsoft.AspNetCore.App", new SemanticVersion("10.0.1"), "/usr/lib/dotnet"),
                new DotNetRuntimeVersion("Microsoft.NETCore.App", new SemanticVersion("10.0.1"), "/home/user/.dotnet"),
                new DotNetRuntimeVersion("Microsoft.NETCore.App", new SemanticVersion("10.0.1"), "/usr/lib/dotnet")
            ],
            runtimes);
    }

    [Fact]
    public void RuntimeVersions_ListARuntimeOnce_WhenTheSameRootListsItTwice()
    {
        var output = ListRuntimesOutput("Microsoft.NETCore.App 10.0.1 [/missing/shared/Microsoft.NETCore.App]");

        var runtimes = DotNetInfo.DistinctRuntimeVersions(
            DotNetInfo.ParseRuntimeVersions(output, "/usr/lib/dotnet")
                .Concat(DotNetInfo.ParseRuntimeVersions(output, "/usr/lib/dotnet")));

        Assert.Equal(
            [new DotNetRuntimeVersion("Microsoft.NETCore.App", new SemanticVersion("10.0.1"), "/usr/lib/dotnet")],
            runtimes);
    }

    private static string ListRuntimesOutput(params string[] lines) => string.Join(Environment.NewLine, lines);
}
