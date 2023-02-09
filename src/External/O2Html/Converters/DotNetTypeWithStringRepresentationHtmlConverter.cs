using System;
using O2Html.Dom;
using O2Html.Dom.Elements;

namespace O2Html.Converters;

public class DotNetTypeWithStringRepresentationHtmlConverter : HtmlConverter
{
    public override Node WriteHtml<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        if (obj == null)
            return new Null().WithAddClass(htmlSerializer.SerializerSettings.CssClasses.Null);

        var str = obj.ToString()?
            .ReplaceIfExists("&", Consts.HtmlAmpersand)
            .ReplaceIfExists(" ", Consts.HtmlSpace)
            .ReplaceIfExists("<", Consts.HtmlLessThan)
            .ReplaceIfExists(">", Consts.HtmlGreaterThan)
            .ReplaceIfExists("\"", Consts.HtmlQuote)
            .ReplaceIfExists("'", Consts.HtmlApostrophe)
            .ReplaceIfExists("\n", Consts.HtmlNewLine);

        return new TextNode(str);
    }

    public override void WriteHtmlWithinTableRow<T>(Element tr, T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        tr.AddAndGetElement("td")
            .WithAddClass(htmlSerializer.SerializerSettings.CssClasses.PropertyValue)
            .AddChild(WriteHtml(obj, type, serializationScope, htmlSerializer));
    }

    public override bool CanConvert(HtmlSerializer htmlSerializer, Type type)
    {
        return htmlSerializer.GetTypeCategory(type) == TypeCategory.DotNetTypeWithStringRepresentation;
    }
}
