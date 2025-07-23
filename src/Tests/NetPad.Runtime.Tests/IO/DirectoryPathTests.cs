using NetPad.IO;
using Xunit;

namespace NetPad.Runtime.Tests.IO;

public class DirectoryPathTests
{
    [Fact]
    public void ImplicitConversion()
    {
        const string path = "/path/to/dir";

        DirectoryPath dirPath = path;
        var normalized = dirPath.Path.Replace('/', Path.DirectorySeparatorChar);

        var expected = Path.GetFullPath(path);
        Assert.Equal(expected, normalized);
    }

    [Fact]
    public void Combine()
    {
        const string path = "/path/to/dir";
        var dirPath = new DirectoryPath(path);

        var newPath = dirPath.Combine("dir1/dir2");
        var normalized = newPath.Path.Replace('/', Path.DirectorySeparatorChar);

        var expected = Path.GetFullPath($"{path}/dir1/dir2");
        Assert.Equal(expected, normalized);
    }

    [Fact]
    public void CombineFilePath()
    {
        const string path = "/path/to/dir";
        var dirPath = new DirectoryPath(path);

        var newPath = dirPath.CombineFilePath("dir1/file1");
        var normalized = newPath.Path.Replace('/', Path.DirectorySeparatorChar);

        var expected = Path.GetFullPath($"{path}/dir1/file1");
        Assert.Equal(expected, normalized);
    }
}
