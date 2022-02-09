using System;
using System.Collections;
using System.Linq;
using O2Html.Dom;
using O2Html.Dom.Elements;

namespace O2Html.Converters;

public class CollectionHtmlConverter : HtmlConverter
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

        var enumerable = GetEnumerable(obj!);

        var collection = enumerable.Cast<object?>().ToArray();

        var table = new Table().WithAddClass(htmlSerializer.SerializerSettings.CssClasses.Table);;

        var oType = collection.FirstOrDefault(x => x != null)?.GetType();
        if (oType != null && oType.IsObjectType())
        {
            var properties = oType.GetReadableProperties().ToArray();
            foreach (var property in properties)
            {
                table.AddAndGetHeading(property.Name, property.PropertyType.GetReadableName(withNamespace: false, forHtml: true))
                    .WithAddClass(htmlSerializer.SerializerSettings.CssClasses.PropertyName);
            }

            var countHeaderRow = table.Head.InsertAndGetChild(0, new Element("tr"));
            countHeaderRow
                .AddAndGetElement("th")
                .WithAddClass("table-item-count")
                .SetOrAddAttribute("colspan", properties.Length.ToString()).Element
                .AddText($"({collection.Length} items)");
        }
        else
        {
            table.AddAndGetHeading($"{enumerable.GetType().GetReadableName(withNamespace: false, forHtml: true)} ({collection.Length} items)");
        }

        foreach (var item in collection)
        {
            var tr = table.Body.AddAndGetElement("tr");
            htmlSerializer.SerializeWithinTableRow(tr, item, serializationScope);
        }

        return table;
    }

    public override void WriteHtmlWithinTableRow<T>(Element tr, T obj, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        var td = tr.AddAndGetElement("td");

        if (obj == null)
        {
            td.AddAndGetNull().WithAddClass(htmlSerializer.SerializerSettings.CssClasses.Null);
            return;
        }

        var enumerable = GetEnumerable(obj!);
        td.AddChild(WriteHtml(enumerable, serializationScope, htmlSerializer));
    }

    public override bool CanConvert(Type type)
    {
        return type.IsCollectionType();
    }

    private IEnumerable GetEnumerable(object obj)
    {
        return obj as IEnumerable ??
               throw new InvalidCastException($"Cannot cast {nameof(obj)} to {nameof(IEnumerable)}");
    }
}
