using System;
using O2Html.Dom;
using O2Html.Dom.Elements;

namespace O2Html.Converters;

public class DotNetTypeWithStringRepresentationHtmlConverter : HtmlConverter
{
    public override Element WriteHtml<T>(T obj, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        if (obj == null)
            return new Null().WithAddClass(htmlSerializer.SerializerSettings.CssClasses.Null);

        var str = obj.ToString()?
            .Replace("\n", "<br/>")
            .Replace(" ", "&nbsp;");

        return new Element("span").WithText(str ?? string.Empty);
    }

    public override void WriteHtmlWithinTableRow<T>(Element tr, T obj, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        tr.AddAndGetElement("td").AddChild(WriteHtml(obj, serializationScope, htmlSerializer));
    }

    public override bool CanConvert(Type type)
    {
        return type.IsDotNetTypeWithStringRepresentation();
    }
}
