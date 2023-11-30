using NetPad.Media;
using O2Html;
using O2Html.Dom;

namespace NetPad.Presentation.Html;

public class VideoHtmlConverter : HtmlConverter
{
    public override bool CanConvert(Type type)
    {
        return typeof(Video).IsAssignableFrom(type);
    }

    public override Node WriteHtml<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        if (obj is not Video video)
            throw new Exception($"Expected an object of type {typeof(Video).FullName}, got {obj.GetType().FullName}");

        string title = video.FilePath?.Path ?? video.Uri?.ToString() ?? (video.Base64Data == null ? "(no source)" : "Base 64 data");

        var videoElement = new Element("video").SetTitle($"Video: {title}");
        videoElement.GetOrAddAttribute("controls");

        videoElement.AddAndGetElement("source")
            .SetSrc(video.HtmlSource);

        return videoElement;
    }

    public override void WriteHtmlWithinTableRow<T>(Element tr, T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        HtmlExtensions.AddChild(tr.AddAndGetElement("td"), WriteHtml(obj, type, serializationScope, htmlSerializer));
    }
}
