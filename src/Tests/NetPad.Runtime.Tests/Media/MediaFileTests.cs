using NetPad.Media;

namespace NetPad.Runtime.Tests.Media;

public class MediaFileTests
{
    // --- Factory methods (FromPath, FromUri, FromBase64, FromBytes) ---

    [Fact]
    public void Image_FromPath_SetsFilePath()
    {
        var image = Image.FromPath("/tmp/photo.png");

        Assert.Equal("/tmp/photo.png", image.FilePath);
        Assert.True(image.IsLocalFile);
        Assert.Null(image.Uri);
        Assert.Null(image.Base64Data);
    }

    [Fact]
    public void Image_FromUri_SetsUri()
    {
        var uri = new Uri("https://example.com/photo.png");
        var image = Image.FromUri(uri);

        Assert.Equal(uri, image.Uri);
        Assert.False(image.IsLocalFile);
        Assert.Null(image.FilePath);
    }

    [Fact]
    public void Image_FromBase64_SetsBase64Data()
    {
        var base64 = "data:image/png;base64,iVBOR";
        var image = Image.FromBase64(base64);

        Assert.Equal(base64, image.Base64Data);
        Assert.False(image.IsLocalFile);
        Assert.Null(image.FilePath);
        Assert.Null(image.Uri);
    }

    [Fact]
    public void Image_FromBytes_CreatesBase64String()
    {
        var bytes = new byte[] { 1, 2, 3, 4 };
        var image = Image.FromBytes(bytes, "image/png");

        Assert.NotNull(image.Base64Data);
        Assert.StartsWith("data:image/png;base64,", image.Base64Data);
        Assert.Contains(Convert.ToBase64String(bytes), image.Base64Data);
    }

    [Fact]
    public void Audio_FromPath_SetsFilePath()
    {
        var audio = Audio.FromPath("/tmp/song.mp3");

        Assert.Equal("/tmp/song.mp3", audio.FilePath);
        Assert.True(audio.IsLocalFile);
    }

    [Fact]
    public void Video_FromUri_SetsUri()
    {
        var uri = new Uri("https://example.com/clip.mp4");
        var video = Video.FromUri(uri);

        Assert.Equal(uri, video.Uri);
        Assert.False(video.IsLocalFile);
    }

    // --- HtmlSource ---

    [Fact]
    public void HtmlSource_ForFilePath_IsEscapedFileUrl()
    {
        var image = Image.FromPath("/tmp/my photo.png");

        Assert.StartsWith("/files/", image.HtmlSource);
        Assert.Contains("my%20photo.png", image.HtmlSource);
    }

    [Fact]
    public void HtmlSource_ForUri_IsUriString()
    {
        var uri = new Uri("https://example.com/photo.png");
        var image = Image.FromUri(uri);

        Assert.Equal("https://example.com/photo.png", image.HtmlSource);
    }

    [Fact]
    public void HtmlSource_ForBase64_IsBase64String()
    {
        var base64 = "data:image/png;base64,abc123";
        var image = Image.FromBase64(base64);

        Assert.Equal(base64, image.HtmlSource);
    }

    [Fact]
    public void HtmlSource_IsEmpty_WhenDefaultConstructed()
    {
        var image = new Image();

        Assert.Equal(string.Empty, image.HtmlSource);
    }

    // --- Constructor overloads ---

    [Fact]
    public void Constructor_WithFilePath_SetsFilePath()
    {
        var image = new Image("/tmp/photo.png");

        Assert.Equal("/tmp/photo.png", image.FilePath);
    }

    [Fact]
    public void Constructor_WithUri_SetsUri()
    {
        var uri = new Uri("https://example.com/photo.png");
        var image = new Image(uri);

        Assert.Equal(uri, image.Uri);
        Assert.Equal("https://example.com/photo.png", image.HtmlSource);
    }

    // --- DisplayWidth / DisplayHeight / extensions ---

    [Fact]
    public void WithDisplayWidth_SetsAndReturnsSameInstance()
    {
        var image = Image.FromPath("/tmp/photo.png");

        var returned = image.WithDisplayWidth("200px");

        Assert.Same(image, returned);
        Assert.Equal("200px", image.DisplayWidth);
    }

    [Fact]
    public void WithDisplayHeight_SetsAndReturnsSameInstance()
    {
        var image = Image.FromPath("/tmp/photo.png");

        var returned = image.WithDisplayHeight("100px");

        Assert.Same(image, returned);
        Assert.Equal("100px", image.DisplayHeight);
    }

    [Fact]
    public void WithDisplaySize_SetsBothDimensions()
    {
        var image = Image.FromPath("/tmp/photo.png")
            .WithDisplaySize("300px", "200px");

        Assert.Equal("300px", image.DisplayWidth);
        Assert.Equal("200px", image.DisplayHeight);
    }

    // --- Open / OpenAndWait throw for Base64-only ---

    [Fact]
    public void Open_ThrowsForBase64Only()
    {
        var image = Image.FromBase64("data:image/png;base64,abc");

        Assert.Throws<InvalidOperationException>(() => image.Open());
    }

    [Fact]
    public void OpenAndWait_ThrowsForBase64Only()
    {
        var image = Image.FromBase64("data:image/png;base64,abc");

        Assert.Throws<InvalidOperationException>(() => image.OpenAndWait());
    }

    // --- IsLocalFile ---

    [Fact]
    public void IsLocalFile_True_WhenFilePathIsSet()
    {
        Assert.True(Image.FromPath("/tmp/photo.png").IsLocalFile);
    }

    [Fact]
    public void IsLocalFile_False_WhenUriIsSet()
    {
        Assert.False(Image.FromUri(new Uri("https://example.com/photo.png")).IsLocalFile);
    }

    [Fact]
    public void IsLocalFile_False_WhenBase64IsSet()
    {
        Assert.False(Image.FromBase64("data:image/png;base64,abc").IsLocalFile);
    }
}
