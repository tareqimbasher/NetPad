using System;
using System.Linq;
using O2Html.Common;
using O2Html.Dom;
using O2Html.Dom.Elements;

namespace O2Html.Converters;

public class TwoDimensionalArrayHtmlConverter : CollectionHtmlConverter
{
    public override bool CanConvert(HtmlSerializer htmlSerializer, Type type)
    {
        return type.IsArray && type.GetArrayRank() == 2;
    }

    protected override (Node node, int? collectionLength) Convert<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        if (obj == null)
            return (new Null(htmlSerializer.SerializerSettings.CssClasses.Null), null);

        if (ShouldShortCircuit(obj, type, serializationScope, htmlSerializer, out var shortCircuitValue))
        {
            return (shortCircuitValue, null);
        }

        if (obj is not Array { Rank: 2 } array)
            throw new HtmlSerializationException($"The {nameof(TwoDimensionalArrayHtmlConverter)} can only convert 2-D arrays");

        Type elementType = htmlSerializer.GetCollectionElementType(type) ?? typeof(object);

        int collectionLength = array.Length;
        int rowCount = array.GetLength(0);
        int columnCount = array.GetLength(1);

        var table = new Table();

        // First heading cell should be empty
        table.Head.WithHeading(string.Empty);

        for (int i = 0; i < columnCount; i++)
        {
            table.Head.WithHeading(i.ToString());
        }

        table.Head.ChildElements.Single().WithAddClass(htmlSerializer.SerializerSettings.CssClasses.TableDataHeader);

        var infoHeaderRow = table.Head.InsertAndGetChild(0, new Element("tr"));
        infoHeaderRow
            .WithAddClass(htmlSerializer.SerializerSettings.CssClasses.TableInfoHeader)
            .AddAndGetElement("th")
            // columnCount + 1 because we added an extra empty cell in the table header
            .SetOrAddAttribute("colspan", (columnCount + 1).ToString()).Element
            .AddText($"{elementType.GetReadableName()}[{rowCount},{columnCount}] ({collectionLength} items)");

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
