using System;
using System.Data;
using O2Html.Common;
using O2Html.Dom;
using O2Html.Dom.Elements;

namespace O2Html.Converters;

public class DataSetHtmlConverter : HtmlConverter
{
    public override bool CanConvert(HtmlSerializer htmlSerializer, Type type)
    {
        return typeof(DataSet) == type;
    }

    public override Node WriteHtml<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        if (obj == null)
            return new Null(htmlSerializer.SerializerSettings.CssClasses.Null);

        if (ShouldShortCircuit(obj, type, serializationScope, htmlSerializer, out var shortCircuitValue))
        {
            return shortCircuitValue;
        }

        if (obj is not DataSet dataSet)
            throw new HtmlSerializationException($"The {nameof(DataSetHtmlConverter)} can only convert objects of type {nameof(DataSet)}");

        var table = new Table();

        var enumerationResult = LazyEnumerable.Enumerate<DataTable>(dataSet.Tables, htmlSerializer.SerializerSettings.MaxCollectionSerializeLength, (dataTable, ix) =>
        {
            var tr = table.Body.AddAndGetRow();

            tr.AddAndGetElement("th").AddText((ix + 1).ToString());

            tr.AddAndGetElement("td").AddChild(htmlSerializer.Serialize(dataTable, typeof(DataTable), serializationScope));
        });

        string headerRowText = (!string.IsNullOrWhiteSpace(dataSet.DataSetName) ? dataSet.DataSetName : "DataTable") +
                               $" ({(enumerationResult.CollectionLengthExceedsMax ? "First " : "")}{enumerationResult.ElementsEnumerated} tables)";

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
