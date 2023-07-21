using System;
using System.Data;
using O2Html.Common;
using O2Html.Dom;
using O2Html.Dom.Elements;

namespace O2Html.Converters;

public class DataTableHtmlConverter : HtmlConverter
{
    public override bool CanConvert(HtmlSerializer htmlSerializer, Type type)
    {
        return typeof(DataTable) == type;
    }

    public override Node WriteHtml<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        if (obj == null)
            return new Null(htmlSerializer.SerializerSettings.CssClasses.Null);

        if (ShouldShortCircuit(obj, type, serializationScope, htmlSerializer, out var shortCircuitValue))
        {
            return shortCircuitValue;
        }

        if (obj is not DataTable dataTable)
            throw new HtmlSerializationException($"The {nameof(DataTableHtmlConverter)} can only convert objects of type {nameof(DataTable)}");

        var table = new Table();
        foreach (DataColumn column in dataTable.Columns)
        {
            table.Head.WithHeading(column.ColumnName, column.DataType.GetReadableName(true));
        }

        var enumerationResult = LazyEnumerable.Enumerate<DataRow>(dataTable.Rows, htmlSerializer.SerializerSettings.MaxCollectionSerializeLength, (row, _) =>
        {
            var tr = table.Body.AddAndGetRow();

            int ixItem = 0;

            foreach (var item in row!.ItemArray)
            {
                var td = tr.AddAndGetElement("td");

                var itemType = item?.GetType() ?? dataTable.Columns[ixItem].DataType;

                if (item == null || itemType == typeof(DBNull))
                {
                    td.AddChild(htmlSerializer.Serialize<object>(null, itemType, serializationScope));
                }
                else
                {
                    td.AddChild(htmlSerializer.Serialize(item, itemType, serializationScope));
                }

                ixItem++;
            }
        });

        string headerRowText = (!string.IsNullOrWhiteSpace(dataTable.TableName) ? dataTable.TableName : "DataTable") +
                               $" ({(enumerationResult.CollectionLengthExceedsMax ? "First " : "")}{enumerationResult.ElementsEnumerated} rows)";

        var infoHeaderRow = table.Head.InsertAndGetChild(0, new Element("tr"));
        infoHeaderRow
            .WithAddClass(htmlSerializer.SerializerSettings.CssClasses.TableInfoHeader)
            .AddAndGetElement("th")
            .SetOrAddAttribute("colspan", dataTable.Columns.Count.ToString()).Element
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
