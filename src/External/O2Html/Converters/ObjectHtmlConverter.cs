using System;
using System.Reflection;
using O2Html.Dom;
using O2Html.Dom.Elements;

namespace O2Html.Converters;

public class ObjectHtmlConverter : HtmlConverter
{
    public override bool CanConvert(HtmlSerializer htmlSerializer, Type type)
    {
        return htmlSerializer.GetTypeCategory(type) == TypeCategory.SingleObject;
    }

    public override Node WriteHtml<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        if (obj == null)
            return new Null(htmlSerializer.SerializerSettings.CssClasses.Null);

        if (ShouldShortCircuit(obj, type, serializationScope, htmlSerializer, out var shortCircuitValue))
        {
            return shortCircuitValue;
        }

        var table = new Table();

        table.Head
            .AddAndGetRow()
            .WithAddClass(htmlSerializer.SerializerSettings.CssClasses.TableInfoHeader)
            .AddAndGetElement("th").SetOrAddAttribute("colspan", "2").Element
            .WithText(type.GetReadableName())
            .WithTitle(type.GetReadableName(true));

        var properties = htmlSerializer.GetReadableProperties(type);

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
                .WithAddClass(htmlSerializer.SerializerSettings.CssClasses.PropertyName)
                .WithTitle($"[{propertyType.GetReadableName(true)}] {name}")
                .AddText($"{name}");

            // Add property value
            tr.AddAndGetElement("td")
                .WithAddClass(htmlSerializer.SerializerSettings.CssClasses.PropertyValue)
                .AddChild(htmlSerializer.Serialize(value, propertyType, serializationScope));
        }

        return table;
    }

    public override void WriteHtmlWithinTableRow<T>(Element tr, T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        if (obj == null)
        {
            tr.AddAndGetElement("td")
                .WithAddClass(htmlSerializer.SerializerSettings.CssClasses.PropertyValue)
                .AddAndGetNull().WithAddClass(htmlSerializer.SerializerSettings.CssClasses.Null);
            return;
        }

        var properties = htmlSerializer.GetReadableProperties(type);

        foreach (var property in properties)
        {
            object? value = GetPropertyValue(property, ref obj!);
            var propertyType = value == null || property.PropertyType != typeof(object)
                ? property.PropertyType
                : value.GetType();

            tr.AddAndGetElement("td")
                .WithAddClass(htmlSerializer.SerializerSettings.CssClasses.PropertyValue)
                .AddChild(htmlSerializer.Serialize(value, propertyType, serializationScope));
        }
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
