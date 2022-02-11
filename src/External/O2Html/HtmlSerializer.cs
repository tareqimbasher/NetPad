using System;
using System.Collections.Generic;
using System.Linq;
using O2Html.Dom;
using O2Html.Converters;

namespace O2Html;

public class HtmlSerializer
{
    public HtmlSerializer(HtmlSerializerSettings? serializerSettings = null)
    {
        SerializerSettings = serializerSettings ?? new HtmlSerializerSettings();
        Converters = new List<HtmlConverter>();

        // Add default converters in this order, first converter in list
        // that can convert object takes precedence
        Converters.Add(new DotNetTypeWithStringRepresentationHtmlConverter());
        Converters.Add(new CollectionHtmlConverter());
        Converters.Add(new ObjectHtmlConverter());

        if (SerializerSettings.Converters?.Any() == true)
        {
            // Insert settings converters at the beginning so they take precedence
            // if user wants to remove one of the default converters they will have to do it manually
            for (int i = 0; i < SerializerSettings.Converters.Count; i++)
            {
                Converters.Insert(i, SerializerSettings.Converters[i]);
            }
        }
    }

    public HtmlSerializerSettings SerializerSettings { get; }

    public List<HtmlConverter> Converters { get; }

    public Element Serialize<T>(T? obj, SerializationScope? serializationScope = null)
    {
        var type = obj?.GetType() ?? typeof(T);
        var converter = GetConverter(type);
        if (converter == null)
            throw new HtmlSerializationException($"Could not find a convert for object of type: {type}");

        serializationScope = GetSerializationScope<T>(obj, serializationScope);

        return converter.WriteHtml(obj, serializationScope, this);
    }

    public void SerializeWithinTableRow<T>(Element tr, T? obj, SerializationScope? serializationScope = null)
    {
        var type = obj?.GetType() ?? typeof(T);
        var converter = GetConverter(type);
        if (converter == null)
            throw new HtmlSerializationException($"Could not find an {nameof(HtmlConverter)} for object of type: {type}");

        serializationScope = GetSerializationScope<T>(obj, serializationScope);

        converter.WriteHtmlWithinTableRow(tr, obj, serializationScope, this);
    }

    private HtmlConverter? GetConverter(Type type)
    {
        for (int i = 0; i < Converters.Count; i++)
        {
            var converter = Converters[i];
            if (converter.CanConvert(type))
                return converter;
        }

        return null;
    }

    private SerializationScope GetSerializationScope<T>(T? obj, SerializationScope? serializationScope)
    {
        if (serializationScope == null)
            serializationScope = new SerializationScope();
        else
        {
            bool createNewScope = obj != null && obj.GetType().IsObjectType();

            if (createNewScope)
                serializationScope = new SerializationScope(serializationScope);
        }

        return serializationScope;
    }

    public static HtmlSerializer Create(HtmlSerializerSettings? serializerSettings = null)
    {
        return new HtmlSerializer(serializerSettings);
    }
}
