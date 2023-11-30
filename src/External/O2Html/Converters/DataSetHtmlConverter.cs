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

            tr.AddAndGetElement("th").AddText((ix + 1).ToString());

            tr.AddAndGetElement("td").AddChild(htmlSerializer.Serialize(dataTable, typeof(DataTable), serializationScope));
        });

        string headerRowText = (!string.IsNullOrWhiteSpace(dataSet.DataSetName) ? dataSet.DataSetName : "DataTable") +
                               $" ({(enumerationResult.CollectionLengthExceedsMax ? "First " : "")}{enumerationResult.ItemsProcessed} tables)";

        table.Head
            .AddAndGetRow()
            .AddClass(htmlSerializer.SerializerOptions.CssClasses.TableInfoHeader)
            .AddAndGetElement("th").SetAttribute("colspan", "2")
            .AddEscapedText(headerRowText);

        return table;
    }

    public override void WriteHtmlWithinTableRow<T>(Element tr, T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        tr.AddAndGetElement("td")
            .AddClass(htmlSerializer.SerializerOptions.CssClasses.PropertyValue)
            .AddChild(WriteHtml(obj, type, serializationScope, htmlSerializer));
    }
}
