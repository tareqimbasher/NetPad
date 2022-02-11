using System;
using O2Html.Dom;
using O2Html.Dom.Elements;

namespace O2Html.Converters;

public class ObjectHtmlConverter : HtmlConverter
{
    public override Element WriteHtml<T>(T obj, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        if (obj == null)
            return new Null().WithAddClass(htmlSerializer.SerializerSettings.CssClasses.Null);

        if (serializationScope.CheckAddAddIsAlreadySerialized(obj))
        {
            var referenceLoopHandling = htmlSerializer.SerializerSettings.ReferenceLoopHandling;
            if (referenceLoopHandling == ReferenceLoopHandling.IgnoreAndSerializeCyclicReference)
                return new CyclicReference(obj).WithAddClass(htmlSerializer.SerializerSettings.CssClasses.CyclicReference);
            else if (referenceLoopHandling == ReferenceLoopHandling.Ignore)
                return new Element("div");
            else if (referenceLoopHandling == ReferenceLoopHandling.Error)
                throw new HtmlSerializationException($"A reference loop was detected. Object already serialized: {obj.GetType().FullName}");
        }

        var oType = obj.GetType();
        var properties = oType.GetReadableProperties();

        var table = new Table().WithAddClass(htmlSerializer.SerializerSettings.CssClasses.Table);

        table.Head.AddAndGetElement("tr")
            .AddAndGetElement("th").SetOrAddAttribute("colspan", "2").Element
            .AddText(oType.FullName!);

        foreach (var property in properties)
        {
            var name = property.Name;
            var value = property.GetValue(obj);

            var tr = table.Body.AddAndGetElement("tr");
            tr.AddAndGetElement("td")
                .AddAndGetElement("span")
                .WithAddClass(htmlSerializer.SerializerSettings.CssClasses.PropertyName)
                .WithTitle(property.PropertyType.GetReadableName(withNamespace: true, forHtml: true))
                .AddText($"{name}: ");

            var valueTd = tr.AddAndGetElement("td");
            valueTd.AddChild(htmlSerializer.Serialize(value, serializationScope));
        }

        return table;
    }

    public override void WriteHtmlWithinTableRow<T>(Element tr, T obj, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        if (obj == null)
        {
            tr.AddAndGetElement("td").AddAndGetNull().WithAddClass(htmlSerializer.SerializerSettings.CssClasses.Null);
            return;
        }

        var properties = obj.GetReadableProperties();

        foreach (var property in properties)
        {
            var td = tr.AddAndGetElement("td");
            var value = property.GetValue(obj);
            td.AddChild(htmlSerializer.Serialize(value, serializationScope));
        }
    }

    public override bool CanConvert(Type type)
    {
        return type.IsObjectType();
    }
}
