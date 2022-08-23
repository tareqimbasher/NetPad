using System;
using System.Collections;
using O2Html.Dom;
using O2Html.Dom.Elements;

namespace O2Html.Converters;

public class CollectionHtmlConverter : HtmlConverter
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

        var collection = ToEnumerable(obj);

        Type elementType = htmlSerializer.GetElementType(type) ?? typeof(object);

        var table = new Table().WithAddClass(htmlSerializer.SerializerSettings.CssClasses.Table);

        int collectionLength = 0;

        foreach (var item in collection)
        {
            collectionLength++;
            var tr = table.Body.AddAndGetElement("tr");
            htmlSerializer.SerializeWithinTableRow(tr, item, elementType, serializationScope);
        }

        if (htmlSerializer.GetTypeCategory(elementType) == TypeCategory.SingleObject)
        {
            var properties = htmlSerializer.GetReadableProperties(elementType);
            foreach (var property in properties)
            {
                table.AddAndGetHeading(property.Name, property.PropertyType.GetReadableName(withNamespace: true, forHtml: true))
                    .WithAddClass(htmlSerializer.SerializerSettings.CssClasses.PropertyName);
            }

            var countHeaderRow = table.Head.InsertAndGetChild(0, new Element("tr"));
            countHeaderRow
                .AddAndGetElement("th")
                .WithAddClass("table-item-count")
                .SetOrAddAttribute("colspan", properties.Length.ToString()).Element
                .AddText($"({collectionLength} items)");
        }
        else
        {
            table.AddAndGetHeading($"{type.GetReadableName(withNamespace: false, forHtml: true)} ({collectionLength} items)");
        }

        return table;
    }

    public override void WriteHtmlWithinTableRow<T>(Element tr, T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        var td = tr.AddAndGetElement("td");

        if (obj == null)
        {
            td.AddAndGetNull().WithAddClass(htmlSerializer.SerializerSettings.CssClasses.Null);
            return;
        }

        var enumerable = ToEnumerable(obj);
        td.AddChild(WriteHtml(enumerable, type, serializationScope, htmlSerializer));
    }

    public override bool CanConvert(HtmlSerializer htmlSerializer, Type type)
    {
        return htmlSerializer.GetTypeCategory(type) == TypeCategory.Collection;
    }

    private IEnumerable ToEnumerable<T>(T obj)
    {
        return obj as IEnumerable ??
               throw new InvalidCastException($"Cannot cast {nameof(obj)} to {nameof(IEnumerable)}");
    }
}
