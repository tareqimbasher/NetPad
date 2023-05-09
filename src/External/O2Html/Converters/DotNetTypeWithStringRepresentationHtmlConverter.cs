using System;
using O2Html.Dom;
using O2Html.Dom.Elements;

namespace O2Html.Converters;

public class DotNetTypeWithStringRepresentationHtmlConverter : HtmlConverter
{
    public override bool CanConvert(HtmlSerializer htmlSerializer, Type type)
    {
        return htmlSerializer.GetTypeCategory(type) == TypeCategory.DotNetTypeWithStringRepresentation;
    }

    public override Node WriteHtml<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        if (obj == null)
            return new Null(htmlSerializer.SerializerSettings.CssClasses.Null);

        return new TextNode(obj.ToString());
    }

    public override void WriteHtmlWithinTableRow<T>(Element tr, T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        tr.AddAndGetElement("td")
            .WithAddClass(htmlSerializer.SerializerSettings.CssClasses.PropertyValue)
            .AddChild(WriteHtml(obj, type, serializationScope, htmlSerializer));
    }
}
