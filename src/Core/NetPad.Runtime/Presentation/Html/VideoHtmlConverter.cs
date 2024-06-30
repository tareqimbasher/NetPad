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
            throw new Exception($"Expected an object of type {typeof(Video).FullName}, got {obj?.GetType().FullName}");

        string title = video.FilePath ?? video.Uri?.ToString() ?? (video.Base64Data == null ? "(no source)" : "Base 64 data");

        var element = new Element("video").SetTitle($"Video: {title}");
        element.GetOrAddAttribute("controls");

        element.AddAndGetElement("source")
            .SetSrc(video.HtmlSource);

        if (!string.IsNullOrWhiteSpace(video.DisplayWidth))
        {
            element.GetOrAddAttribute("style").Append($"width: {video.DisplayWidth};");
        }

        if (!string.IsNullOrWhiteSpace(video.DisplayHeight))
        {
            element.GetOrAddAttribute("style").Append($"height: {video.DisplayHeight};");
        }

        return element;
    }

    public override void WriteHtmlWithinTableRow<T>(Element tr, T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        tr.AddAndGetElement("td").AddChild(WriteHtml(obj, type, serializationScope, htmlSerializer));
    }
}
