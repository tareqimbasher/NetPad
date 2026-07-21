using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using O2Html.Dom;
using O2Html.Dom.Elements;
using Xunit;

namespace O2Html.Tests;

/// <summary>
/// The structural markup every converter emits around its values: the item count, the data header
/// that names the columns, and the cells that label a row.
/// </summary>
public class TableMarkupTests
{
    [Theory]
    [MemberData(nameof(ItemCountShapes))]
    public void EveryTableCountsWhatItIsShowing(object value, string expected)
    {
        var count = Assert.Single(ItemCounts(value));

        Assert.Equal(expected, count.Text());
    }

    public static IEnumerable<object[]> ItemCountShapes()
    {
        yield return new object[] { new[] { "one", "two" }, "2 items" };
        yield return new object[] { new[] { new Reading(1), new Reading(2), new Reading(3) }, "3 items" };
        yield return new object[] { new[,] { { 1, 2 }, { 3, 4 } }, "4 items" };
        yield return new object[] { (1, "two"), "2 items" };
        yield return new object[] { new Memory<int>(new[] { 1, 2, 3 }), "3 items" };
    }

    [Fact]
    public void ADataTableCountsRowsAndADataSetCountsTables()
    {
        // The DataSet's own count comes first; each nested table counts its rows after it.
        var counts = ItemCounts(TwoTableDataSet()).Select(c => c.Text()).ToArray();

        Assert.Equal(new[] { "2 tables", "1 rows", "1 rows" }, counts);
    }

    [Theory]
    [MemberData(nameof(TruncatedShapes))]
    public void ATruncatedTableSaysSoInItsCount(object value, string expected)
    {
        var options = new HtmlSerializerOptions { MaxCollectionSerializeLength = 2 };

        var count = ItemCounts(value, options).First();

        Assert.Equal(expected, count.Text());
    }

    public static IEnumerable<object[]> TruncatedShapes()
    {
        yield return new object[] { new[] { "one", "two", "three" }, "First 2 items" };
        yield return new object[] { (1, 2, 3), "First 2 items" };
        yield return new object[] { ThreeRowTable(), "First 2 rows" };
    }

    [Fact]
    public void TheItemCountCssClassIsConfigurable()
    {
        var options = new HtmlSerializerOptions();
        options.CssClasses.ItemCount = "count";

        var header = Assert.Single(TestHtml.Tags(new[] { "one" }, "th", options));

        Assert.Contains(header.ChildElements, e => e.HasClass("count"));
    }

    /// <summary>
    /// A tuple holding exactly the maximum number of items is not truncated. The loop this replaced
    /// stopped one item early and then reported the short count as complete.
    /// </summary>
    [Theory]
    [InlineData(3, "3 items", 3)]
    [InlineData(4, "First 3 items", 3)]
    public void ATupleIsTruncatedOnlyWhenItHasMoreItemsThanTheMax(int tupleLength, string expectedCount, int expectedRows)
    {
        var options = new HtmlSerializerOptions { MaxCollectionSerializeLength = 3 };

        object tuple = tupleLength == 3 ? (1, 2, 3) : (1, 2, 3, 4);

        var node = HtmlSerializer.Serialize(tuple, options);

        var count = Assert.Single(TestHtml.Descendants(node), e => e.HasClass("item-count"));
        Assert.Equal(expectedCount, count.Text());

        var labels = TestHtml.Descendants(node).Where(e => e.TagName == "th" && e.HasClass("property-name")).ToArray();
        Assert.Equal(expectedRows, labels.Length);
        Assert.Equal($"Item{expectedRows}", labels.Last().Text());
    }

    [Theory]
    [MemberData(nameof(DataHeaderShapes))]
    public void ATableWithColumnsMarksTheRowThatHeadsThem(object value, string[] expectedHeadings)
    {
        var headerRow = Assert.Single(DataHeaderRows(value));

        Assert.Equal(expectedHeadings, headerRow.ChildElements.Select(th => th.Text()).ToArray());
    }

    public static IEnumerable<object[]> DataHeaderShapes()
    {
        yield return new object[] { new[] { new Reading(1) }, new[] { "Value" } };
        yield return new object[] { ThreeRowTable(), new[] { "Sensor", "Value" } };
        // A 2-D array heads its columns with their index, and its first heading cell is empty.
        yield return new object[] { new[,] { { 1, 2 } }, new[] { "", "0", "1" } };
    }

    [Theory]
    [MemberData(nameof(NamedColumnShapes))]
    public void AColumnHeadingThatNamesSomethingSaysSo(object value)
    {
        var headerRow = Assert.Single(DataHeaderRows(value));

        Assert.All(headerRow.ChildElements, th => Assert.True(th.HasClass("property-name")));
    }

    public static IEnumerable<object[]> NamedColumnShapes()
    {
        yield return new object[] { new[] { new Reading(1) } };
        yield return new object[] { ThreeRowTable() };
    }

    [Fact]
    public void ANestedDataTableAlsoMarksItsDataHeader()
    {
        // Two tables in the set, so two data headers.
        Assert.Equal(2, DataHeaderRows(TwoTableDataSet()).Count());
    }

    [Fact]
    public void ADataTableWithNoColumnsHasNoDataHeaderToMark()
    {
        var node = HtmlSerializer.Serialize(new DataTable("Empty"));

        Assert.DoesNotContain(TestHtml.Descendants(node), e => e.HasClass("table-data-header"));
        Assert.Equal("0 rows", Assert.Single(TestHtml.Descendants(node), e => e.HasClass("item-count")).Text());
    }

    [Theory]
    [MemberData(nameof(RowLabelShapes))]
    public void EveryCellThatLabelsARowSaysSo(object value, string[] expectedLabels)
    {
        var labels = TestHtml.Tags(value, "th")
            .Where(th => th.HasClass("property-name") && th.Parent?.Parent is TBody)
            .Select(th => th.Text())
            .ToArray();

        Assert.Equal(expectedLabels, labels);
    }

    public static IEnumerable<object[]> RowLabelShapes()
    {
        yield return new object[] { new Reading(1), new[] { "Value" } };
        yield return new object[] { (1, "two"), new[] { "Item1", "Item2" } };
        yield return new object[] { new[,] { { 1, 2 }, { 3, 4 } }, new[] { "0", "1" } };
        yield return new object[] { TwoTableDataSet(), new[] { "1", "2" } };
    }

    private static IEnumerable<Element> ItemCounts(object value, HtmlSerializerOptions? options = null)
    {
        return TestHtml.Descendants(HtmlSerializer.Serialize(value, options))
            .Where(e => e.HasClass("item-count"));
    }

    private static IEnumerable<Element> DataHeaderRows(object value)
    {
        return TestHtml.Descendants(HtmlSerializer.Serialize(value))
            .Where(e => e.HasClass("table-data-header"));
    }

    private static DataTable ThreeRowTable()
    {
        var table = new DataTable("Readings");

        table.Columns.Add("Sensor", typeof(string));
        table.Columns.Add("Value", typeof(decimal));

        table.Rows.Add("north", 1.5m);
        table.Rows.Add("south", 2.5m);
        table.Rows.Add("east", 3.5m);

        return table;
    }

    private static DataSet TwoTableDataSet()
    {
        var dataSet = new DataSet("Set");

        for (var i = 0; i < 2; i++)
        {
            var table = new DataTable($"Table {i}");
            table.Columns.Add("Sensor", typeof(string));
            table.Rows.Add("north");
            dataSet.Tables.Add(table);
        }

        return dataSet;
    }

    private record Reading(int Value);
}
