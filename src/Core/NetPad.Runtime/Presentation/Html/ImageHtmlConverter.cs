using NetPad.Media;
using O2Html;
using O2Html.Dom;

namespace NetPad.Presentation.Html;

public class ImageHtmlConverter : HtmlConverter
{
    public override bool CanConvert(Type type)
    {
        return typeof(Image).IsAssignableFrom(type);
    }

    public override Node WriteHtml<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        if (obj is not Image image)
            throw new Exception($"Expected an object of type {typeof(Image).FullName}, got {obj?.GetType().FullName}");

        string title = image.FilePath ?? image.Uri?.ToString() ?? (image.Base64Data == null ? "(no source)" : "Base 64 data");

        var element = new Element("<img />")
            .SetSrc(image.HtmlSource)
            .SetAttribute("alt", title)
            .SetTitle($"Image: {title}");

        if (!string.IsNullOrWhiteSpace(image.DisplayWidth))
        {
            element.GetOrAddAttribute("style").Append($"width: {image.DisplayWidth};");
        }

        if (!string.IsNullOrWhiteSpace(image.DisplayHeight))
        {
            element.GetOrAddAttribute("style").Append($"height: {image.DisplayHeight};");
        }

        return element;
    }

    public override void WriteHtmlWithinTableRow<T>(Element tr, T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        tr.AddAndGetElement("td").AddChild(WriteHtml(obj, type, serializationScope, htmlSerializer));
    }
}
