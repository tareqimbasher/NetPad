using NetPad.Utilities;

namespace NetPad.Runtime.Tests.Utilities;

public class FileSystemUtilTests : IDisposable
{
    private readonly string _tempDir;

    public FileSystemUtilTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "NetPad_Tests", nameof(FileSystemUtilTests),
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    // GetReadableFileSize

    [Theory]
    [InlineData(0, "0 B ")]
    [InlineData(512, "512 B ")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(1073741824, "1 GB")]
    public void GetReadableFileSize_FormatsCorrectly(long bytes, string expected)
    {
        var result = FileSystemUtil.GetReadableFileSize(bytes);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetReadableFileSize_ThrowsForNegativeBytes()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            FileSystemUtil.GetReadableFileSize(-1));
    }

    [Fact]
    public void GetReadableFileSize_RespectsDecimalPlaces()
    {
        // 1.5 KB = 1536 bytes
        var result = FileSystemUtil.GetReadableFileSize(1536, decimalPlaces: 1);

        Assert.Equal("1.5 KB", result);
    }

    // IsDirectoryReadable

    [Fact]
    public void IsDirectoryReadable_ReturnsTrueForReadableDirectory()
    {
        Assert.True(FileSystemUtil.IsDirectoryReadable(_tempDir));
    }

    [Fact]
    public void IsDirectoryReadable_ReturnsFalseForNonExistentDirectory()
    {
        Assert.False(FileSystemUtil.IsDirectoryReadable(Path.Combine(_tempDir, "nonexistent")));
    }

    // IsDirectoryWritable

    [Fact]
    public void IsDirectoryWritable_ReturnsTrueForWritableDirectory()
    {
        Assert.True(FileSystemUtil.IsDirectoryWritable(_tempDir));
    }

    [Fact]
    public void IsDirectoryWritable_ReturnsFalseForNonExistentDirectory()
    {
        Assert.False(FileSystemUtil.IsDirectoryWritable(Path.Combine(_tempDir, "nonexistent")));
    }

    // CopyDirectory

    [Fact]
    public void CopyDirectory_CopiesFiles()
    {
        var sourceDir = Path.Combine(_tempDir, "source");
        var destDir = Path.Combine(_tempDir, "dest");
        Directory.CreateDirectory(sourceDir);
        File.WriteAllText(Path.Combine(sourceDir, "file.txt"), "hello");

        FileSystemUtil.CopyDirectory(sourceDir, destDir, recursive: false);

        Assert.True(File.Exists(Path.Combine(destDir, "file.txt")));
        Assert.Equal("hello", File.ReadAllText(Path.Combine(destDir, "file.txt")));
    }

    [Fact]
    public void CopyDirectory_Recursive_CopiesSubdirectories()
    {
        var sourceDir = Path.Combine(_tempDir, "source");
        var subDir = Path.Combine(sourceDir, "sub");
        var destDir = Path.Combine(_tempDir, "dest");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(sourceDir, "root.txt"), "root");
        File.WriteAllText(Path.Combine(subDir, "nested.txt"), "nested");

        FileSystemUtil.CopyDirectory(sourceDir, destDir, recursive: true);

        Assert.True(File.Exists(Path.Combine(destDir, "root.txt")));
        Assert.True(File.Exists(Path.Combine(destDir, "sub", "nested.txt")));
        Assert.Equal("nested", File.ReadAllText(Path.Combine(destDir, "sub", "nested.txt")));
    }

    [Fact]
    public void CopyDirectory_NonRecursive_DoesNotCopySubdirectories()
    {
        var sourceDir = Path.Combine(_tempDir, "source");
        var subDir = Path.Combine(sourceDir, "sub");
        var destDir = Path.Combine(_tempDir, "dest");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "nested.txt"), "nested");

        FileSystemUtil.CopyDirectory(sourceDir, destDir, recursive: false);

        Assert.False(Directory.Exists(Path.Combine(destDir, "sub")));
    }

    [Fact]
    public void CopyDirectory_ThrowsForNonExistentSource()
    {
        var sourceDir = Path.Combine(_tempDir, "nonexistent");
        var destDir = Path.Combine(_tempDir, "dest");

        Assert.Throws<DirectoryNotFoundException>(() =>
            FileSystemUtil.CopyDirectory(sourceDir, destDir, recursive: false));
    }
}
