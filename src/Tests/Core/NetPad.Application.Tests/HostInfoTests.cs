using System.Reflection;
using Xunit;

namespace NetPad.Application.Tests;

public class HostInfoTests
{
    [Fact]
    public void DefaultsHostUrl()
    {
        var hostInfo = new HostInfo();

        Assert.Equal("http://localhost", hostInfo.HostUrl);
    }

    [Fact]
    public void DefaultsWorkingDirectory()
    {
        var hostInfo = new HostInfo();

        Assert.Equal(Assembly.GetEntryAssembly()!.Location, hostInfo.WorkingDirectory);
    }

    [Fact]
    public void SetsHostUrl()
    {
        var hostInfo = new HostInfo();
        hostInfo.SetHostUrl("Some value");

        Assert.Equal("Some value", hostInfo.HostUrl);
    }

    [Fact]
    public void SetsWorkingDirectoryUrl()
    {
        var hostInfo = new HostInfo();
        hostInfo.SetWorkingDirectory("Some value");

        Assert.Equal("Some value", hostInfo.WorkingDirectory);
    }
}
