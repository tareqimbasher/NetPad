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

        Assert.Equal(path, dirPath.Path);
    }

    [Fact]
    public void Combine()
    {
        const string path = "/path/to/dir";
        var dirPath = new DirectoryPath(path);

        var newPath = dirPath.Combine("dir1/dir2");

        Assert.Equal("/path/to/dir/dir1/dir2", newPath.Path);
    }

    [Fact]
    public void CombineFilePath()
    {
        const string path = "/path/to/dir";
        var dirPath = new DirectoryPath(path);

        var newPath = dirPath.CombineFilePath("dir1/file1");

        Assert.Equal("/path/to/dir/dir1/file1", newPath.Path);
    }
}
