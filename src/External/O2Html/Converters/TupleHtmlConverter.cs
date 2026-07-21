#if NETSTANDARD2_1 || NETCOREAPP3_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using O2Html.Common;
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

        var enumerationResult = Enumerate.Max(
            Items(tuple),
            htmlSerializer.SerializerOptions.MaxCollectionSerializeLength,
            (item, ix) =>
            {
                var tr = table.Body.AddAndGetRow();

                tr.AddAndGetElement("th")
                    .AddClass(htmlSerializer.SerializerOptions.CssClasses.PropertyName)
                    .AddText($"Item{ix + 1}");

                var itemType = item?.GetType() ?? typeof(object);

                htmlSerializer.AddAndGetValueCell(tr, item)
                    .AddChild(htmlSerializer.Serialize(item, itemType, serializationScope));
            });

        table.Head
            .AddAndGetRow()
            .AddClass(htmlSerializer.SerializerOptions.CssClasses.TableInfoHeader)
            .AddAndGetElement("th").SetAttribute("colspan", "2")
            .AddText(type.GetReadableName())
            .AddItemCount(htmlSerializer, enumerationResult.ItemsProcessed, "items", enumerationResult.CollectionLengthExceedsMax);

        return table;
    }

    private static IEnumerable<object?> Items(ITuple tuple)
    {
        for (int i = 0; i < tuple.Length; i++)
        {
            yield return tuple[i];
        }
    }

    public override void WriteHtmlWithinTableRow<T>(TableRow tr, T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        htmlSerializer.AddAndGetValueCell(tr, obj)
            .AddChild(WriteHtml(obj, type, serializationScope, htmlSerializer));
    }
}
#endif
