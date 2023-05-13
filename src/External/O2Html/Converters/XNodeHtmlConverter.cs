using System;
using System.Xml.Linq;
using O2Html.Dom;
using O2Html.Dom.Elements;

namespace O2Html.Converters;

public class XNodeHtmlConverter : DotNetTypeWithStringRepresentationHtmlConverter
{
    public override bool CanConvert(HtmlSerializer htmlSerializer, Type type)
    {
        return typeof(XNode).IsAssignableFrom(type);
    }

    public override Node WriteHtml<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        if (obj == null)
            return new Null(htmlSerializer.SerializerSettings.CssClasses.Null);

        if (obj is not XNode xNode)
            throw new HtmlSerializationException($"The {nameof(XNodeHtmlConverter)} can only convert objects of type {nameof(XNode)}");

        return base.WriteHtml(xNode.ToString(), typeof(string), serializationScope, htmlSerializer);
    }
}
