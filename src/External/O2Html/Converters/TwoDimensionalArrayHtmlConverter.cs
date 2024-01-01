using System;
using System.Linq;
using O2Html.Dom;
using O2Html.Dom.Elements;

namespace O2Html.Converters;

public class TwoDimensionalArrayHtmlConverter : CollectionHtmlConverter
{
    public override bool CanConvert(Type type)
    {
        return type.IsArray && type.GetArrayRank() == 2;
    }

    protected override (Node node, int? collectionLength) Convert<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        if (obj is not Array { Rank: 2 } array)
            throw new HtmlSerializationException($"The {nameof(TwoDimensionalArrayHtmlConverter)} can only convert 2-D arrays");

        Type elementType = HtmlSerializer.GetCollectionElementType(type) ?? typeof(object);

        int collectionLength = array.Length;
        int rowCount = array.GetLength(0);
        int columnCount = array.GetLength(1);

        var table = new Table();

        // First heading cell should be empty
        table.Head.AddHeading(string.Empty);

        for (int i = 0; i < columnCount; i++)
        {
            table.Head.AddHeading(i.ToString());
        }

        table.Head.ChildElements.Single().AddClass(htmlSerializer.SerializerOptions.CssClasses.TableDataHeader);

        var infoHeaderRow = table.Head.InsertAndGetChild(0, new Element("tr"));
        infoHeaderRow
            .AddClass(htmlSerializer.SerializerOptions.CssClasses.TableInfoHeader)
            .AddAndGetElement("th")
            // columnCount + 1 because we added an extra empty cell in the table header
            .SetAttribute("colspan", (columnCount + 1).ToString())
            .AddEscapedText($"{elementType.GetReadableName()}[{rowCount},{columnCount}] ({collectionLength} items)");

        for (int iRow = 0; iRow < rowCount; iRow++)
        {
            var tr = table.Body.AddAndGetRow();
            tr.AddAndGetElement("th").AddText(iRow.ToString());

            for (int iColumn = 0; iColumn < columnCount; iColumn++)
            {
                htmlSerializer.SerializeWithinTableRow(tr, array.GetValue(iRow, iColumn), elementType, serializationScope);
            }
        }

        return (table, collectionLength);
    }
}
