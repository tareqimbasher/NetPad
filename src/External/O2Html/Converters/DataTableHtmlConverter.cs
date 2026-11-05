using System;
using System.Data;
using System.Linq;
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
            table.Head
                .AddAndGetHeading(column.ColumnName, column.DataType.GetReadableName(true))
                .AddClass(htmlSerializer.SerializerOptions.CssClasses.PropertyName);
        }

        // A DataTable with no columns has no heading row to mark.
        if (dataTable.Columns.Count > 0)
        {
            table.Head.ChildElements.Single().AddClass(htmlSerializer.SerializerOptions.CssClasses.TableDataHeader);
        }

        var enumerationResult = Enumerate.Max<DataRow>(dataTable.Rows, htmlSerializer.SerializerOptions.MaxCollectionSerializeLength, (row, _) =>
        {
            var tr = table.Body.AddAndGetRow();

            int ixItem = 0;

            foreach (var item in row.ItemArray)
            {
                var itemType = item?.GetType() ?? dataTable.Columns[ixItem].DataType;

                if (item == null || itemType == typeof(DBNull))
                {
                    htmlSerializer.AddAndGetValueCell<object>(tr, null)
                        .AddChild(htmlSerializer.Serialize<object>(null, itemType, serializationScope));
                }
                else
                {
                    htmlSerializer.AddAndGetValueCell(tr, item)
                        .AddChild(htmlSerializer.Serialize(item, itemType, serializationScope));
                }

                ixItem++;
            }
        });

        string headerRowText = !string.IsNullOrWhiteSpace(dataTable.TableName) ? dataTable.TableName : "DataTable";

        var infoHeaderRow = table.Head.InsertAndGetChild(0, new Element("tr"));
        infoHeaderRow
            .AddClass(htmlSerializer.SerializerOptions.CssClasses.TableInfoHeader)
            .AddAndGetElement("th")
            .SetAttribute("colspan", dataTable.Columns.Count.ToString())
            .AddEscapedText(headerRowText)
            .AddItemCount(htmlSerializer, enumerationResult.ItemsProcessed, "rows", enumerationResult.CollectionLengthExceedsMax);

        return table;
    }

    public override void WriteHtmlWithinTableRow<T>(TableRow tr, T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        htmlSerializer.AddAndGetValueCell(tr, obj)
            .AddChild(WriteHtml(obj, type, serializationScope, htmlSerializer));
    }
}
