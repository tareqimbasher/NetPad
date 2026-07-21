using System;
using O2Html.Dom;
using O2Html.Dom.Elements;

namespace O2Html.Converters;

public class DotNetTypeWithStringRepresentationHtmlConverter : HtmlConverter
{
    public override bool CanConvert(Type type)
    {
        return HtmlSerializer.GetTypeCategory(type) == TypeCategory.DotNetTypeWithStringRepresentation;
    }

    public override Node WriteHtml<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        var text = obj is bool booleanValue
            ? booleanValue ? "true" : "false"
            : obj!.ToString()!;

        return TextNode.EscapedText(text);
    }

    public override void WriteHtmlWithinTableRow<T>(TableRow tr, T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        htmlSerializer.AddAndGetValueCell(tr, obj)
            .AddChild(WriteHtml(obj, type, serializationScope, htmlSerializer));
    }
}
