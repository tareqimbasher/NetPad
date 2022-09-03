using System;
using O2Html.Dom;
using O2Html.Dom.Elements;

namespace O2Html.Converters;

public class DotNetTypeWithStringRepresentationHtmlConverter : HtmlConverter
{
    public override Element WriteHtml<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        if (obj == null)
            return new Null().WithAddClass(htmlSerializer.SerializerSettings.CssClasses.Null);

        var str = obj.ToString()?
            .ReplaceIfExists(" ", "&nbsp;")
            .ReplaceIfExists("<", "&lt;")
            .ReplaceIfExists(">", "&gt;")
            .ReplaceIfExists("\n", "<br/>");

        return new Element("span").WithText(str);
    }

    public override void WriteHtmlWithinTableRow<T>(Element tr, T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        tr.AddAndGetElement("td").AddChild(WriteHtml(obj, type, serializationScope, htmlSerializer));
    }

    public override bool CanConvert(HtmlSerializer htmlSerializer, Type type)
    {
        return htmlSerializer.GetTypeCategory(type) == TypeCategory.DotNetTypeWithStringRepresentation;
    }
}
