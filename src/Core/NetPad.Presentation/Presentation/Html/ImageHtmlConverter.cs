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
            throw new Exception($"Expected an object of type {typeof(Image).FullName}, got {obj.GetType().FullName}");

        string title = image.FilePath?.Path ?? image.Uri?.ToString() ?? (image.Base64Data == null ? "(no source)" : "Base 64 data");

        return new Element("<img />")
            .SetSrc(image.HtmlSource)
            .SetAttribute("alt", title)
            .SetTitle($"Image: {title}");
    }

    public override void WriteHtmlWithinTableRow<T>(Element tr, T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        HtmlExtensions.AddChild(tr.AddAndGetElement("td"), WriteHtml(obj, type, serializationScope, htmlSerializer));
    }
}
