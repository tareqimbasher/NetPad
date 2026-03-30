using NetPad.Media;
using NetPad.Presentation.Html;
using O2Html;

namespace NetPad.Runtime.Tests.Html;

public class ImageHtmlConverterTests
{
    private readonly ImageHtmlConverter _converter = new();
    private readonly HtmlSerializer _serializer = new();
    private readonly SerializationScope _scope = new(0);

    [Fact]
    public void CanConvert_Image_ReturnsTrue()
    {
        Assert.True(_converter.CanConvert(typeof(Image)));
    }

    [Fact]
    public void CanConvert_NonImage_ReturnsFalse()
    {
        Assert.False(_converter.CanConvert(typeof(Audio)));
        Assert.False(_converter.CanConvert(typeof(Video)));
        Assert.False(_converter.CanConvert(typeof(string)));
    }

    [Fact]
    public void WriteHtml_FromPath_RendersImgTag()
    {
        var image = Image.FromPath("/tmp/photo.png");

        var html = _converter.WriteHtml(image, typeof(Image), _scope, _serializer).ToHtml();

        Assert.Contains("<img", html);
        Assert.Contains("src=", html);
        Assert.Contains("/files/", html);
    }

    [Fact]
    public void WriteHtml_FromUri_RendersImgWithUriSrc()
    {
        var image = Image.FromUri(new Uri("https://example.com/photo.png"));

        var html = _converter.WriteHtml(image, typeof(Image), _scope, _serializer).ToHtml();

        Assert.Contains("src=\"https://example.com/photo.png\"", html);
    }

    [Fact]
    public void WriteHtml_FromBase64_RendersImgWithBase64Src()
    {
        var image = Image.FromBase64("data:image/png;base64,abc123");

        var html = _converter.WriteHtml(image, typeof(Image), _scope, _serializer).ToHtml();

        Assert.Contains("src=\"data:image/png;base64,abc123\"", html);
    }

    [Fact]
    public void WriteHtml_SetsAltAndTitle()
    {
        var image = Image.FromPath("/tmp/photo.png");

        var html = _converter.WriteHtml(image, typeof(Image), _scope, _serializer).ToHtml();

        Assert.Contains("alt=\"/tmp/photo.png\"", html);
        Assert.Contains("title=\"Image: /tmp/photo.png\"", html);
    }

    [Fact]
    public void WriteHtml_WithDisplaySize_AddsStyleAttribute()
    {
        var image = Image.FromPath("/tmp/photo.png")
            .WithDisplaySize("200px", "100px");

        var html = _converter.WriteHtml(image, typeof(Image), _scope, _serializer).ToHtml();

        Assert.Contains("width: 200px;", html);
        Assert.Contains("height: 100px;", html);
    }

    [Fact]
    public void WriteHtml_WithoutDisplaySize_NoStyleAttribute()
    {
        var image = Image.FromPath("/tmp/photo.png");

        var html = _converter.WriteHtml(image, typeof(Image), _scope, _serializer).ToHtml();

        Assert.DoesNotContain("style=", html);
    }

    [Fact]
    public void WriteHtml_DefaultConstructed_ShowsNoSourceTitle()
    {
        var image = new Image();

        var html = _converter.WriteHtml(image, typeof(Image), _scope, _serializer).ToHtml();

        Assert.Contains("(no source)", html);
    }
}

public class AudioHtmlConverterTests
{
    private readonly AudioHtmlConverter _converter = new();
    private readonly HtmlSerializer _serializer = new();
    private readonly SerializationScope _scope = new(0);

    [Fact]
    public void CanConvert_Audio_ReturnsTrue()
    {
        Assert.True(_converter.CanConvert(typeof(Audio)));
    }

    [Fact]
    public void CanConvert_NonAudio_ReturnsFalse()
    {
        Assert.False(_converter.CanConvert(typeof(Image)));
        Assert.False(_converter.CanConvert(typeof(Video)));
    }

    [Fact]
    public void WriteHtml_RendersAudioTagWithControls()
    {
        var audio = Audio.FromPath("/tmp/song.mp3");

        var html = _converter.WriteHtml(audio, typeof(Audio), _scope, _serializer).ToHtml();

        Assert.Contains("<audio", html);
        Assert.Contains("controls", html);
        Assert.Contains("src=", html);
    }

    [Fact]
    public void WriteHtml_FromUri_RendersSrcFromUri()
    {
        var audio = Audio.FromUri(new Uri("https://example.com/song.mp3"));

        var html = _converter.WriteHtml(audio, typeof(Audio), _scope, _serializer).ToHtml();

        Assert.Contains("src=\"https://example.com/song.mp3\"", html);
    }

    [Fact]
    public void WriteHtml_SetsTitle()
    {
        var audio = Audio.FromPath("/tmp/song.mp3");

        var html = _converter.WriteHtml(audio, typeof(Audio), _scope, _serializer).ToHtml();

        Assert.Contains("title=\"Audio: /tmp/song.mp3\"", html);
    }

    [Fact]
    public void WriteHtml_WithDisplaySize_AddsStyleAttribute()
    {
        var audio = Audio.FromPath("/tmp/song.mp3")
            .WithDisplaySize("300px", "50px");

        var html = _converter.WriteHtml(audio, typeof(Audio), _scope, _serializer).ToHtml();

        Assert.Contains("width: 300px;", html);
        Assert.Contains("height: 50px;", html);
    }
}

public class VideoHtmlConverterTests
{
    private readonly VideoHtmlConverter _converter = new();
    private readonly HtmlSerializer _serializer = new();
    private readonly SerializationScope _scope = new(0);

    [Fact]
    public void CanConvert_Video_ReturnsTrue()
    {
        Assert.True(_converter.CanConvert(typeof(Video)));
    }

    [Fact]
    public void CanConvert_NonVideo_ReturnsFalse()
    {
        Assert.False(_converter.CanConvert(typeof(Image)));
        Assert.False(_converter.CanConvert(typeof(Audio)));
    }

    [Fact]
    public void WriteHtml_RendersVideoTagWithControls()
    {
        var video = Video.FromPath("/tmp/clip.mp4");

        var html = _converter.WriteHtml(video, typeof(Video), _scope, _serializer).ToHtml();

        Assert.Contains("<video", html);
        Assert.Contains("controls", html);
    }

    [Fact]
    public void WriteHtml_RendersSourceChildElement()
    {
        var video = Video.FromUri(new Uri("https://example.com/clip.mp4"));

        var html = _converter.WriteHtml(video, typeof(Video), _scope, _serializer).ToHtml();

        Assert.Contains("<source", html);
        Assert.Contains("src=\"https://example.com/clip.mp4\"", html);
    }

    [Fact]
    public void WriteHtml_SetsTitle()
    {
        var video = Video.FromPath("/tmp/clip.mp4");

        var html = _converter.WriteHtml(video, typeof(Video), _scope, _serializer).ToHtml();

        Assert.Contains("title=\"Video: /tmp/clip.mp4\"", html);
    }

    [Fact]
    public void WriteHtml_WithDisplaySize_AddsStyleAttribute()
    {
        var video = Video.FromPath("/tmp/clip.mp4")
            .WithDisplaySize("640px", "480px");

        var html = _converter.WriteHtml(video, typeof(Video), _scope, _serializer).ToHtml();

        Assert.Contains("width: 640px;", html);
        Assert.Contains("height: 480px;", html);
    }
}

public class MediaFileHtmlConverterTests
{
    private readonly MediaFileHtmlConverter _converter = new();

    [Fact]
    public void CanConvert_MediaFile_ReturnsTrue()
    {
        Assert.True(_converter.CanConvert(typeof(MediaFile)));
    }

    [Fact]
    public void CanConvert_DerivedTypes_ReturnsFalse()
    {
        // MediaFileHtmlConverter only handles the abstract base type directly
        Assert.False(_converter.CanConvert(typeof(Image)));
        Assert.False(_converter.CanConvert(typeof(Audio)));
        Assert.False(_converter.CanConvert(typeof(Video)));
    }
}

public class MediaFileCollectionConverterTests
{
    private readonly MediaFileCollectionConverter _converter = new();

    [Fact]
    public void CanConvert_ImageArray_ReturnsTrue()
    {
        Assert.True(_converter.CanConvert(typeof(Image[])));
    }

    [Fact]
    public void CanConvert_AudioList_ReturnsTrue()
    {
        Assert.True(_converter.CanConvert(typeof(List<Audio>)));
    }

    [Fact]
    public void CanConvert_VideoEnumerable_ReturnsTrue()
    {
        Assert.True(_converter.CanConvert(typeof(IEnumerable<Video>)));
    }

    [Fact]
    public void CanConvert_NonMediaCollection_ReturnsFalse()
    {
        Assert.False(_converter.CanConvert(typeof(string[])));
        Assert.False(_converter.CanConvert(typeof(List<int>)));
    }

    [Fact]
    public void CanConvert_NonCollection_ReturnsFalse()
    {
        Assert.False(_converter.CanConvert(typeof(Image)));
        Assert.False(_converter.CanConvert(typeof(string)));
    }
}
