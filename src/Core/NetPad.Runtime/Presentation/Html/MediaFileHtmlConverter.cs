using NetPad.Media;
using O2Html;
using O2Html.Dom;
using O2Html.Dom.Elements;

namespace NetPad.Presentation.Html;

public class MediaFileHtmlConverter : HtmlConverter
{
    public override bool CanConvert(Type type)
    {
        return type == typeof(MediaFile);
    }

    public override Node WriteHtml<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        if (obj is Image image) return htmlSerializer.Serialize(image, typeof(Image), serializationScope);
        if (obj is Audio audio) return htmlSerializer.Serialize(audio, typeof(Audio), serializationScope);
        if (obj is Video video) return htmlSerializer.Serialize(video, typeof(Video), serializationScope);

        throw new Exception($"Unhandled {nameof(MediaFile)} type.");
    }

    public override void WriteHtmlWithinTableRow<T>(TableRow tr, T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        htmlSerializer.AddAndGetValueCell(tr, obj).AddChild(WriteHtml(obj, type, serializationScope, htmlSerializer));
    }
}
