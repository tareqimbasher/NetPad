#if NETSTANDARD2_1 || NETCOREAPP3_0_OR_GREATER
using System;
using System.Runtime.CompilerServices;
using O2Html.Dom;
using O2Html.Dom.Elements;

namespace O2Html.Converters;

public class TupleHtmlConverter : HtmlConverter
{
    public override bool CanConvert(HtmlSerializer htmlSerializer, Type type)
    {
        return typeof(ITuple).IsAssignableFrom(type);
    }

    public override Node WriteHtml<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        if (obj == null)
            return new Null(htmlSerializer.SerializerSettings.CssClasses.Null);

        if (ShouldShortCircuit(obj, type, serializationScope, htmlSerializer, out var shortCircuitValue))
        {
            return shortCircuitValue;
        }

        if (obj is not ITuple tuple)
            throw new HtmlSerializationException($"The {nameof(DataSetHtmlConverter)} can only convert objects of type {nameof(ITuple)}");

        var table = new Table();

        int serializedItemCount = 0;
        for (int iItem = 0; iItem < tuple.Length; iItem++)
        {
            if (iItem + 1 == htmlSerializer.SerializerSettings.MaxCollectionSerializeLength)
            {
                break;
            }

            var tr = table.Body.AddAndGetRow();

            tr.AddAndGetElement("th").AddText($"Item{iItem + 1}");

            var item = tuple[iItem];
            var itemType = item == null ? typeof(object) : item.GetType();

            tr.AddAndGetElement("td").AddChild(htmlSerializer.Serialize(item, itemType, serializationScope));
            ++serializedItemCount;
        }

        string headerRowText = type.GetReadableName() +
                               $" ({(tuple.Length > htmlSerializer.SerializerSettings.MaxCollectionSerializeLength ? "First " : "")}{serializedItemCount} items)";

        table.Head
            .AddAndGetRow()
            .WithAddClass(htmlSerializer.SerializerSettings.CssClasses.TableInfoHeader)
            .AddAndGetElement("th").SetOrAddAttribute("colspan", "2").Element
            .AddText(headerRowText);

        return table;
    }

    public override void WriteHtmlWithinTableRow<T>(Element tr, T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        tr.AddAndGetElement("td")
            .WithAddClass(htmlSerializer.SerializerSettings.CssClasses.PropertyValue)
            .AddChild(WriteHtml(obj, type, serializationScope, htmlSerializer));
    }
}
#endif
