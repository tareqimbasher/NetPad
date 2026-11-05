using System;
using System.Data;
using O2Html.Common;
using O2Html.Dom;
using O2Html.Dom.Elements;

namespace O2Html.Converters;

public class DataSetHtmlConverter : HtmlConverter
{
    public override bool CanConvert(Type type)
    {
        return typeof(DataSet) == type;
    }

    public override Node WriteHtml<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        if (obj is not DataSet dataSet)
            throw new HtmlSerializationException($"The {nameof(DataSetHtmlConverter)} can only convert objects of type {nameof(DataSet)}");

        var table = new Table();

        var enumerationResult = Enumerate.Max<DataTable>(dataSet.Tables, htmlSerializer.SerializerOptions.MaxCollectionSerializeLength, (dataTable, ix) =>
        {
            var tr = table.Body.AddAndGetRow();

            tr.AddAndGetElement("th")
                .AddClass(htmlSerializer.SerializerOptions.CssClasses.PropertyName)
                .AddText((ix + 1).ToString());

            htmlSerializer.AddAndGetValueCell(tr, dataTable)
                .AddChild(htmlSerializer.Serialize(dataTable, typeof(DataTable), serializationScope));
        });

        string headerRowText = !string.IsNullOrWhiteSpace(dataSet.DataSetName) ? dataSet.DataSetName : "DataSet";

        table.Head
            .AddAndGetRow()
            .AddClass(htmlSerializer.SerializerOptions.CssClasses.TableInfoHeader)
            .AddAndGetElement("th").SetAttribute("colspan", "2")
            .AddEscapedText(headerRowText)
            .AddItemCount(htmlSerializer, enumerationResult.ItemsProcessed, "tables", enumerationResult.CollectionLengthExceedsMax);

        return table;
    }

    public override void WriteHtmlWithinTableRow<T>(TableRow tr, T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        htmlSerializer.AddAndGetValueCell(tr, obj)
            .AddChild(WriteHtml(obj, type, serializationScope, htmlSerializer));
    }
}
