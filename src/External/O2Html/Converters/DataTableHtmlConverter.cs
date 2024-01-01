using System;
using System.Data;
using O2Html.Common;
using O2Html.Dom;
using O2Html.Dom.Elements;

namespace O2Html.Converters;

public class DataTableHtmlConverter : HtmlConverter
{
    public override bool CanConvert(Type type)
    {
        return typeof(DataTable) == type;
    }

    public override Node WriteHtml<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        if (obj is not DataTable dataTable)
            throw new HtmlSerializationException($"The {nameof(DataTableHtmlConverter)} can only convert objects of type {nameof(DataTable)}");

        var table = new Table();
        foreach (DataColumn column in dataTable.Columns)
        {
            table.Head.AddHeading(column.ColumnName, column.DataType.GetReadableName(true));
        }

        var enumerationResult = Enumerate.Max<DataRow>(dataTable.Rows, htmlSerializer.SerializerOptions.MaxCollectionSerializeLength, (row, _) =>
        {
            var tr = table.Body.AddAndGetRow();

            int ixItem = 0;

            foreach (var item in row.ItemArray)
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
                               $" ({(enumerationResult.CollectionLengthExceedsMax ? "First " : "")}{enumerationResult.ItemsProcessed} rows)";

        var infoHeaderRow = table.Head.InsertAndGetChild(0, new Element("tr"));
        infoHeaderRow
            .AddClass(htmlSerializer.SerializerOptions.CssClasses.TableInfoHeader)
            .AddAndGetElement("th")
            .SetAttribute("colspan", dataTable.Columns.Count.ToString())
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
