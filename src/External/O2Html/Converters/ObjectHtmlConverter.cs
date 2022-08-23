using System;
using System.Reflection;
using O2Html.Dom;
using O2Html.Dom.Elements;

namespace O2Html.Converters;

public class ObjectHtmlConverter : HtmlConverter
{
    public override Element WriteHtml<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        if (obj == null)
            return new Null().WithAddClass(htmlSerializer.SerializerSettings.CssClasses.Null);

        if (serializationScope.CheckAddAddIsAlreadySerialized(obj))
        {
            var referenceLoopHandling = htmlSerializer.SerializerSettings.ReferenceLoopHandling;

            if (referenceLoopHandling == ReferenceLoopHandling.IgnoreAndSerializeCyclicReference)
                return new CyclicReference(type).WithAddClass(htmlSerializer.SerializerSettings.CssClasses.CyclicReference);

            if (referenceLoopHandling == ReferenceLoopHandling.Ignore)
                return new Element("div");

            if (referenceLoopHandling == ReferenceLoopHandling.Error)
                throw new HtmlSerializationException($"A reference loop was detected. Object already serialized: {type.FullName}");
        }

        var table = new Table().WithAddClass(htmlSerializer.SerializerSettings.CssClasses.Table);

        table.Head.AddAndGetElement("tr")
            .AddAndGetElement("th").SetOrAddAttribute("colspan", "2").Element
            .AddText(type.GetReadableName(withNamespace: true, forHtml: true));

        var properties = htmlSerializer.GetReadableProperties(type);

        foreach (var property in properties)
        {
            var name = property.Name;
            object? value = GetPropertyValue(property, ref obj!);

            var tr = table.Body.AddAndGetElement("tr");
            tr.AddAndGetElement("td")
                .AddAndGetElement("span")
                .WithAddClass(htmlSerializer.SerializerSettings.CssClasses.PropertyName)
                .WithTitle(property.PropertyType.GetReadableName(withNamespace: true, forHtml: true))
                .AddText($"{name}: ");

            var valueTd = tr.AddAndGetElement("td");
            valueTd.AddChild(htmlSerializer.Serialize(value, property.PropertyType, serializationScope));
        }

        return table;
    }

    public override void WriteHtmlWithinTableRow<T>(Element tr, T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        if (obj == null)
        {
            tr.AddAndGetElement("td").AddAndGetNull().WithAddClass(htmlSerializer.SerializerSettings.CssClasses.Null);
            return;
        }

        var properties = htmlSerializer.GetReadableProperties(type);

        foreach (var property in properties)
        {
            var td = tr.AddAndGetElement("td");
            object? value = GetPropertyValue(property, ref obj!);
            td.AddChild(htmlSerializer.Serialize(value, property.PropertyType, serializationScope));
        }
    }

    public override bool CanConvert(HtmlSerializer htmlSerializer, Type type)
    {
        return htmlSerializer.GetTypeCategory(type) == TypeCategory.SingleObject;
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
