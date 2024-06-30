using NetPad.Media;
using O2Html;
using O2Html.Converters;
using O2Html.Dom;

namespace NetPad.Presentation.Html;

public class AudioHtmlConverter : ObjectHtmlConverter
{
    public override bool CanConvert(Type type)
    {
        return typeof(Audio).IsAssignableFrom(type);
    }

    public override Node WriteHtml<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        if (obj is not Audio audio)
            throw new Exception($"Expected an object of type {typeof(Audio).FullName}, got {obj?.GetType().FullName}");

        string title = audio.FilePath ?? audio.Uri?.ToString() ?? (audio.Base64Data == null ? "(no source)" : "Base 64 data");

        var element = new Element("audio")
            .SetAttribute("controls")
            .SetSrc(audio.HtmlSource)
            .SetTitle($"Audio: {title}");

        if (!string.IsNullOrWhiteSpace(audio.DisplayWidth))
        {
            element.GetOrAddAttribute("style").Append($"width: {audio.DisplayWidth};");
        }

        if (!string.IsNullOrWhiteSpace(audio.DisplayHeight))
        {
            element.GetOrAddAttribute("style").Append($"height: {audio.DisplayHeight};");
        }

        return element;
    }

    public override void WriteHtmlWithinTableRow<T>(Element tr, T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        tr.AddAndGetElement("td").AddChild(WriteHtml(obj, type, serializationScope, htmlSerializer));
    }
}
