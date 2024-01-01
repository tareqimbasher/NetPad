using System;
using System.Reflection;
using O2Html.Dom;
using O2Html.Dom.Elements;

namespace O2Html.Converters;

public class ObjectHtmlConverter : HtmlConverter
{
    public override bool CanConvert(Type type)
    {
        return HtmlSerializer.GetTypeCategory(type) == TypeCategory.SingleObject;
    }

    public override Node WriteHtml<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        var table = new Table();

        table.Head
            .AddAndGetRow()
            .AddClass(htmlSerializer.SerializerOptions.CssClasses.TableInfoHeader)
            .AddAndGetElement("th").SetAttribute("colspan", "2")
            .AddEscapedText(type.GetReadableName())
            .SetTitle(type.GetReadableName(true));

        PropertyInfo[] properties = GetReadableProperties(htmlSerializer, type);

        foreach (var property in properties)
        {
            var name = property.Name;
            object? value = GetPropertyValue(property, ref obj!);

            var propertyType = value == null || property.PropertyType != typeof(object)
                ? property.PropertyType
                : value.GetType();

            var tr = table.Body.AddAndGetRow();

            // Add property name
            tr.AddAndGetElement("th")
                .AddClass(htmlSerializer.SerializerOptions.CssClasses.PropertyName)
                .SetTitle($"[{propertyType.GetReadableName(true)}] {name}")
                .AddText(name);

            // Add property value
            tr.AddAndGetElement("td")
                .AddClass(htmlSerializer.SerializerOptions.CssClasses.PropertyValue)
                .AddChild(htmlSerializer.Serialize(value, propertyType, serializationScope));
        }

        return table;
    }

    public override void WriteHtmlWithinTableRow<T>(Element tr, T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        if (obj == null)
        {
            tr.AddAndGetElement("td")
                .AddClass(htmlSerializer.SerializerOptions.CssClasses.PropertyValue)
                .AddAndGetNull().AddClass(htmlSerializer.SerializerOptions.CssClasses.Null);
            return;
        }

        var properties = HtmlSerializer.GetReadableProperties(type);

        foreach (var property in properties)
        {
            object? value = GetPropertyValue(property, ref obj!);
            var propertyType = value == null || property.PropertyType != typeof(object)
                ? property.PropertyType
                : value.GetType();

            tr.AddAndGetElement("td")
                .AddClass(htmlSerializer.SerializerOptions.CssClasses.PropertyValue)
                .AddChild(htmlSerializer.Serialize(value, propertyType, serializationScope));
        }
    }

    protected virtual PropertyInfo[] GetReadableProperties(HtmlSerializer htmlSerializer, Type type)
    {
        return HtmlSerializer.GetReadableProperties(type);
    }

    private object? GetPropertyValue<T>(PropertyInfo property, ref T? obj)
    {
        try
        {
            return property.GetValue(obj);
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }
}
