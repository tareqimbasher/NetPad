using OmniSharp;
using OmniSharp.Stdio;

namespace NetPad.Plugins.OmniSharp.Tests.Stdio;

public class OmniSharpStdioServerConfigurationTests
{
    [Fact]
    public void ExecutableArgs_IncludeTheSdkVersion_WhenTheSdkHasOne()
    {
        var config = NewConfiguration(new OmniSharpDotNetSdk("/dotnet", "8.0.416"));

        Assert.Equal("-s /project Sdk:Version=8.0.416", config.ExecutableArgs);
    }

    [Fact]
    public void ExecutableArgs_AreUnchanged_WhenTheSdkHasNoVersion()
    {
        var config = NewConfiguration(new OmniSharpDotNetSdk("/dotnet"));

        Assert.Equal("-s /project", config.ExecutableArgs);
    }

    [Fact]
    public void ExecutableArgs_AreUnchanged_WhenThereIsNoSdk()
    {
        var config = NewConfiguration(null);

        Assert.Equal("-s /project", config.ExecutableArgs);
    }

    private static OmniSharpStdioServerConfiguration NewConfiguration(OmniSharpDotNetSdk? dotNetSdk) =>
        new("omnisharp", "-s /project", dotNetSdk, null);
}
