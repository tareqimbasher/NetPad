using System;
using System.IO;
using System.Text;
using System.Xml;
using O2Html.Dom;
using O2Html.Dom.Elements;

namespace O2Html.Converters;

public class XmlNodeHtmlConverter : DotNetTypeWithStringRepresentationHtmlConverter
{
    private static readonly Lazy<XmlWriterSettings> _xmlWriterSettings = new(() => new XmlWriterSettings { Indent = true, Encoding = Encoding.UTF8 });

    public override bool CanConvert(HtmlSerializer htmlSerializer, Type type)
    {
        return typeof(XmlNode).IsAssignableFrom(type);
    }

    public override Node WriteHtml<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        if (obj == null)
            return new Null(htmlSerializer.SerializerSettings.CssClasses.Null);

        if (obj is not XmlNode xmlNode)
            throw new HtmlSerializationException($"The {nameof(XmlNodeHtmlConverter)} can only convert objects of type {nameof(XmlNode)}");

        string xml;

        using (var stringWriter = new StringWriter())
        using (var xmlTextWriter = XmlWriter.Create(stringWriter, _xmlWriterSettings.Value))
        {
            xmlNode.WriteTo(xmlTextWriter);
            xmlTextWriter.Flush();
            xml = stringWriter.ToString();
        }

        return base.WriteHtml(xml, typeof(string), serializationScope, htmlSerializer);
    }
}
