#if NETSTANDARD2_1 || NETCOREAPP3_0_OR_GREATER
using System;
using System.Runtime.CompilerServices;
using O2Html.Dom;
using O2Html.Dom.Elements;

namespace O2Html.Converters;

public class TupleHtmlConverter : HtmlConverter
{
    public override bool CanConvert(Type type)
    {
        return typeof(ITuple).IsAssignableFrom(type);
    }

    public override Node WriteHtml<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        if (obj is not ITuple tuple)
            throw new HtmlSerializationException($"The {nameof(DataSetHtmlConverter)} can only convert objects of type {nameof(ITuple)}");

        var table = new Table();

        int serializedItemCount = 0;
        for (int iItem = 0; iItem < tuple.Length; iItem++)
        {
            if (iItem + 1 == htmlSerializer.SerializerOptions.MaxCollectionSerializeLength)
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
                               $" ({(tuple.Length > htmlSerializer.SerializerOptions.MaxCollectionSerializeLength ? "First " : "")}{serializedItemCount} items)";

        table.Head
            .AddAndGetRow()
            .AddClass(htmlSerializer.SerializerOptions.CssClasses.TableInfoHeader)
            .AddAndGetElement("th").SetAttribute("colspan", "2")
            .AddText(headerRowText);

        return table;
    }

    public override void WriteHtmlWithinTableRow<T>(Element tr, T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        tr.AddAndGetElement("td")
            .AddClass(htmlSerializer.SerializerOptions.CssClasses.PropertyValue)
            .AddChild(WriteHtml(obj, type, serializationScope, htmlSerializer));
    }
}
#endif
