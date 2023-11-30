using System;
using System.Xml.Linq;
using O2Html.Common;
using O2Html.Dom;

namespace O2Html.Converters;

public class XNodeHtmlConverter : DotNetTypeWithStringRepresentationHtmlConverter
{
    public override bool CanConvert(Type type)
    {
        return typeof(XNode).IsAssignableFrom(type);
    }

    public override Node WriteHtml<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        if (obj is not XNode xNode)
            throw new HtmlSerializationException($"The {nameof(XNodeHtmlConverter)} can only convert objects of type {nameof(XNode)}");

        var pre = new Element("pre");
        pre.AddAndGetElement("code")
            .SetAttribute("language", "xml")
            .AddText(Util.EscapeAngleBracketsForHtml(xNode.ToString()));

        return pre;
    }
}
