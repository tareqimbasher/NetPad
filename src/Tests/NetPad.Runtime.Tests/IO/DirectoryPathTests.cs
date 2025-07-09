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

        Assert.Equal(path, normalized);
    }

    [Fact]
    public void Combine()
    {
        const string path = "/path/to/dir";
        var dirPath = new DirectoryPath(path);

        var newPath = dirPath.Combine("dir1/dir2");
        var normalized = newPath.Path.Replace('/', Path.DirectorySeparatorChar);

        Assert.Equal("/path/to/dir/dir1/dir2", normalized);
    }

    [Fact]
    public void CombineFilePath()
    {
        const string path = "/path/to/dir";
        var dirPath = new DirectoryPath(path);

        var newPath = dirPath.CombineFilePath("dir1/file1");
        var normalized = newPath.Path.Replace('/', Path.DirectorySeparatorChar);

        Assert.Equal("/path/to/dir/dir1/file1", normalized);
    }
}
