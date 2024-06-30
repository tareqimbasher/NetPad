using System.Reflection;
using NetPad.Application;
using Xunit;

namespace NetPad.Runtime.Tests.Application;

public class AppIdentifierTests
{
    [Fact]
    public void AppName_IsCorrect()
    {
        Assert.Equal("NetPad", AppIdentifier.AppName);
        Assert.Equal("NetPad", new AppIdentifier().Name);
    }

    [Fact]
    public void AppVersion_ShouldBeVersionOfEntryAssembly()
    {
        var versionOfEntryAssembly = Assembly.GetEntryAssembly()?.GetName().Version;
        Assert.Equal(versionOfEntryAssembly, new AppIdentifier().Version);
    }
}
