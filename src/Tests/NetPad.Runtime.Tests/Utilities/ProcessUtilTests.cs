using NetPad.Utilities;

namespace NetPad.Runtime.Tests.Utilities;

public class ProcessUtilTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveShellExecuteTarget_Rejects_EmptyPath(string? path)
    {
        Assert.Throws<ArgumentException>(() => ProcessUtil.ResolveShellExecuteTarget(path!));
    }

    [Fact]
    public void ResolveShellExecuteTarget_Resolves_ExistingFile()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
        File.WriteAllText(filePath, "test");

        try
        {
            var resolved = ProcessUtil.ResolveShellExecuteTarget(filePath);

            Assert.Equal(Path.GetFullPath(filePath), resolved);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void ResolveShellExecuteTarget_Resolves_ExistingDirectory()
    {
        var dirPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}");
        Directory.CreateDirectory(dirPath);

        try
        {
            var resolved = ProcessUtil.ResolveShellExecuteTarget(dirPath);

            Assert.Equal(Path.GetFullPath(dirPath), resolved);
        }
        finally
        {
            Directory.Delete(dirPath);
        }
    }

    [Fact]
    public void ResolveShellExecuteTarget_Rejects_NonExistentPath()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}", "does-not-exist.txt");

        Assert.Throws<ArgumentException>(() => ProcessUtil.ResolveShellExecuteTarget(path));
    }

    [Theory]
    [InlineData("http://example.com/")]
    [InlineData("https://example.com/docs")]
    [InlineData("mailto:someone@example.com")]
    public void ResolveShellExecuteTarget_Resolves_AllowedUrlSchemes(string url)
    {
        var resolved = ProcessUtil.ResolveShellExecuteTarget(url);

        Assert.Equal(new Uri(url).AbsoluteUri, resolved);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==")]
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("ms-msdt:/id PCWDiagnostic")]
    [InlineData("search-ms:query=x")]
    [InlineData("ftp://example.com/payload")]
    public void ResolveShellExecuteTarget_Rejects_DisallowedUrlSchemes(string url)
    {
        Assert.Throws<ArgumentException>(() => ProcessUtil.ResolveShellExecuteTarget(url));
    }

    [Fact]
    public void ResolveShellExecuteTarget_Rejects_FileUrl_PointingAtNonExistentPath()
    {
        var url = new Uri(Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt")).AbsoluteUri;

        Assert.Throws<ArgumentException>(() => ProcessUtil.ResolveShellExecuteTarget(url));
    }

    [Fact]
    public void ResolveShellExecuteTarget_Resolves_FileUrl_PointingAtExistingFile()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
        File.WriteAllText(filePath, "test");

        try
        {
            var resolved = ProcessUtil.ResolveShellExecuteTarget(new Uri(filePath).AbsoluteUri);

            Assert.Equal(Path.GetFullPath(filePath), resolved);
        }
        finally
        {
            File.Delete(filePath);
        }
    }
}
