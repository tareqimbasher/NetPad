using System.Runtime.InteropServices;
using NetPad.Utilities;

namespace NetPad.Runtime.Tests.Utilities;

public class PlatformUtilTests
{
    [Fact]
    public void GetPlatformExecutableExtension_ReturnsExpected()
    {
        var ext = PlatformUtil.GetPlatformExecutableExtension();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.Equal(".exe", ext);
        else
            Assert.Equal(string.Empty, ext);
    }

    [Fact]
    public void PathSeparator_ReturnsExpected()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            Assert.Equal(';', PlatformUtil.PathSeparator);
        else
            Assert.Equal(':', PlatformUtil.PathSeparator);
    }

    [Fact]
    public void AppendToPathVariable_WithEmptyExisting_ReturnsDirectory()
    {
        var result = PlatformUtil.AppendToPathVariable("", "/usr/bin");

        Assert.Equal("/usr/bin", result);
    }

    [Fact]
    public void AppendToPathVariable_WithNullExisting_ReturnsDirectory()
    {
        var result = PlatformUtil.AppendToPathVariable(null, "/usr/bin");

        Assert.Equal("/usr/bin", result);
    }

    [Fact]
    public void AppendToPathVariable_AppendsWithSeparator()
    {
        var sep = PlatformUtil.PathSeparator;
        var result = PlatformUtil.AppendToPathVariable("/existing", "/new");

        Assert.Equal($"/existing{sep}/new", result);
    }

    [Fact]
    public void AppendToPathVariable_DoesNotDoubleSeparator_WhenExistingEndsWith()
    {
        var sep = PlatformUtil.PathSeparator;
        var result = PlatformUtil.AppendToPathVariable($"/existing{sep}", "/new");

        Assert.Equal($"/existing{sep}/new", result);
    }
}
