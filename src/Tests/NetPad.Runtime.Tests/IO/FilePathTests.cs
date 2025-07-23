using NetPad.IO;
using Xunit;

namespace NetPad.Runtime.Tests.IO;

public class FilePathTests
{
    [Fact]
    public void ImplicitConversion()
    {
        const string path = "/path/to/file";

        FilePath filePath = path;
        var normalized = filePath.Path.Replace('/', Path.DirectorySeparatorChar);

        var expected = Path.GetFullPath(path);
        Assert.Equal(expected, normalized);
    }
}
