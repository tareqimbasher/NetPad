using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Xml.Linq;
using O2Html.Dom;
using Xunit;

namespace O2Html.Tests;

public class ValueCellTests
{
    [Theory]
    [InlineData(typeof(string), ValueKind.Text)]
    [InlineData(typeof(char), ValueKind.Text)]
    [InlineData(typeof(Guid), ValueKind.Text)]
    [InlineData(typeof(Uri), ValueKind.Text)]
    [InlineData(typeof(byte), ValueKind.Numeric)]
    [InlineData(typeof(sbyte), ValueKind.Numeric)]
    [InlineData(typeof(short), ValueKind.Numeric)]
    [InlineData(typeof(ushort), ValueKind.Numeric)]
    [InlineData(typeof(int), ValueKind.Numeric)]
    [InlineData(typeof(uint), ValueKind.Numeric)]
    [InlineData(typeof(long), ValueKind.Numeric)]
    [InlineData(typeof(ulong), ValueKind.Numeric)]
    [InlineData(typeof(float), ValueKind.Numeric)]
    [InlineData(typeof(double), ValueKind.Numeric)]
    [InlineData(typeof(decimal), ValueKind.Numeric)]
    [InlineData(typeof(nint), ValueKind.Numeric)]
    [InlineData(typeof(nuint), ValueKind.Numeric)]
    [InlineData(typeof(bool), ValueKind.Boolean)]
    [InlineData(typeof(BindingFlags), ValueKind.Enum)]
    [InlineData(typeof(DateTime), ValueKind.Temporal)]
    [InlineData(typeof(DateTimeOffset), ValueKind.Temporal)]
    [InlineData(typeof(TimeSpan), ValueKind.Temporal)]
    [InlineData(typeof(DateOnly), ValueKind.Temporal)]
    [InlineData(typeof(TimeOnly), ValueKind.Temporal)]
    [InlineData(typeof(int?), ValueKind.Numeric)]
    [InlineData(typeof(bool?), ValueKind.Boolean)]
    [InlineData(typeof(BindingFlags?), ValueKind.Enum)]
    [InlineData(typeof(DateTime?), ValueKind.Temporal)]
    public void GetValueKindDerivesKindFromType(Type type, ValueKind expected)
    {
        Assert.Equal(expected, HtmlSerializer.GetValueKind(type));
    }

    [Fact]
    public void AnUndefinedEnumMemberIsStillAnEnum()
    {
        var cell = Assert.Single(ValueCells(new { Value = (Status)7 }));

        Assert.Contains("enum", cell.ClassList);
        Assert.DoesNotContain("numeric", cell.ClassList);
    }

    [Theory]
    [InlineData(1, "numeric")]
    [InlineData(true, "boolean-true")]
    [InlineData(false, "boolean-false")]
    [InlineData("text", null)]
    [InlineData('c', null)]
    public void ValueCellsCarryTheirKind(object value, string? expectedClass)
    {
        var cell = Assert.Single(ValueCells(new { Value = value }));

        Assert.Contains("property-value", cell.ClassList);

        if (expectedClass != null) Assert.Contains(expectedClass, cell.ClassList);
        else Assert.Single(cell.ClassList);
    }

    [Fact]
    public void NullValueCellsCarryNoKind()
    {
        var cell = Assert.Single(ValueCells(new { Value = (int?)null }));

        Assert.Equal(new[] { "property-value" }, cell.ClassList);
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    public void NegativeMarksOnlyNumbersBelowZero(int value, bool expectNegative)
    {
        var cell = Assert.Single(ValueCells(new { Value = value }));

        Assert.Equal(expectNegative, cell.ClassList.Contains("negative"));
    }

    [Theory]
    [MemberData(nameof(NegativeValues))]
    public void EveryNumericTypeReportsItsSign(object negative, object positive)
    {
        Assert.Contains("negative", Assert.Single(ValueCells(new { Value = negative })).ClassList);
        Assert.DoesNotContain("negative", Assert.Single(ValueCells(new { Value = positive })).ClassList);
    }

    public static IEnumerable<object[]> NegativeValues()
    {
        yield return new object[] { (sbyte)-1, (sbyte)1 };
        yield return new object[] { (short)-1, (short)1 };
        yield return new object[] { -1, 1 };
        yield return new object[] { -1L, 1L };
        yield return new object[] { -1.5f, 1.5f };
        yield return new object[] { -1.5d, 1.5d };
        yield return new object[] { -1.5m, 1.5m };
        yield return new object[] { (nint)(-1), (nint)1 };
    }

    [Fact]
    public void BooleansAreWrittenAsCSharpLiterals()
    {
        var html = HtmlSerializer.Serialize(new { Yes = true, No = false }).ToHtml();

        Assert.Contains(">true<", html);
        Assert.Contains(">false<", html);
        Assert.DoesNotContain("True", html);
        Assert.DoesNotContain("False", html);
    }

    [Fact]
    public void CssClassNamesAreConfigurable()
    {
        var options = new HtmlSerializerOptions();
        options.CssClasses.Numeric = "num";
        options.CssClasses.Negative = "below-zero";

        var cell = Assert.Single(ValueCells(new { Value = -1 }, options));

        Assert.Contains("num", cell.ClassList);
        Assert.Contains("below-zero", cell.ClassList);
    }

    /// <summary>
    /// The classification this replaced parsed the rendered text, which a culture that renders
    /// decimals as "1,5" defeats.
    /// </summary>
    [Fact]
    public void NumbersAreClassifiedIndependentlyOfCulture()
    {
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");

        try
        {
            var cells = ValueCells(new[]
            {
                new Reading(1.5m, -2.25),
                new Reading(3.75m, 4.5),
            }).ToArray();

            Assert.Equal(4, cells.Length);
            Assert.All(cells, c => Assert.Contains("numeric", c.ClassList));
            Assert.Contains("negative", cells[1].ClassList);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Theory]
    [MemberData(nameof(ConverterCellShapes))]
    public void EveryConverterEmitsClassifiedValueCells(object value, string[] expectedClasses)
    {
        var cells = ValueCells(value).ToArray();

        Assert.All(cells, c => Assert.Contains("property-value", c.ClassList));

        foreach (var expected in expectedClasses)
        {
            Assert.Contains(cells, c => c.ClassList.Contains(expected));
        }
    }

    public static IEnumerable<object[]> ConverterCellShapes()
    {
        // DotNetTypeWithStringRepresentationHtmlConverter (as an item of a collection)
        yield return new object[] { new[] { 1, -2 }, new[] { "numeric", "negative" } };
        yield return new object[] { new[] { true, false }, new[] { "boolean-true", "boolean-false" } };
        yield return new object[] { new[] { DateTime.UtcNow }, new[] { "temporal" } };
        yield return new object[] { new[] { BindingFlags.Public }, new[] { "enum" } };

        // ObjectHtmlConverter, both as the root object and as an item of a collection
        yield return new object[] { new Reading(1.5m, -2.5), new[] { "numeric", "negative" } };
        yield return new object[] { new[] { new Reading(1.5m, -2.5) }, new[] { "numeric", "negative" } };

        // CollectionHtmlConverter (nested collection sits in its own value cell)
        yield return new object[] { new { Values = new[] { 1 } }, new[] { "property-value" } };

        // TwoDimensionalArrayHtmlConverter
        yield return new object[] { new[,] { { 1, -2 }, { 3, 4 } }, new[] { "numeric", "negative" } };

        // TupleHtmlConverter
        yield return new object[] { (1, -2m, true, "text"), new[] { "numeric", "negative", "boolean-true" } };

        // MemoryHtmlConverter
        yield return new object[] { new Memory<int>(new[] { 1, -2 }), new[] { "numeric", "negative" } };

        // DataTableHtmlConverter / DataSetHtmlConverter
        yield return new object[] { ReadingsTable(), new[] { "numeric", "negative", "boolean-true", "temporal" } };
        yield return new object[] { ReadingsDataSet(), new[] { "numeric", "negative", "boolean-true", "temporal" } };

        // FileSystemInfoHtmlConverter (an ObjectHtmlConverter with a curated property set)
        yield return new object[] { new DirectoryInfo("/etc"), new[] { "temporal", "boolean-true", "enum" } };

        // JsonDocumentHtmlConverter, XNodeHtmlConverter and XmlNodeHtmlConverter render a code block,
        // so their cells only carry the value-cell class.
        yield return new object[] { new[] { JsonDocument.Parse("{\"a\": 1}") }, new[] { "property-value" } };
        yield return new object[] { new[] { XElement.Parse("<a/>") }, new[] { "property-value" } };
    }

    [Fact]
    public void JsonAndXmlValueCellsCarryNoKind()
    {
        Assert.Equal(new[] { "property-value" },
            Assert.Single(ValueCells(new[] { JsonDocument.Parse("{\"a\": 1}") })).ClassList);

        Assert.Equal(new[] { "property-value" },
            Assert.Single(ValueCells(new[] { XElement.Parse("<a/>") })).ClassList);
    }

    private static IEnumerable<Element> ValueCells(object value, HtmlSerializerOptions? options = null)
    {
        return TestHtml.Tags(value, "td", options);
    }

    private static DataTable ReadingsTable()
    {
        var table = new DataTable("Readings");

        table.Columns.Add("Amount", typeof(decimal));
        table.Columns.Add("Delta", typeof(double));
        table.Columns.Add("Valid", typeof(bool));
        table.Columns.Add("Taken", typeof(DateTime));
        table.Columns.Add("Note", typeof(string));

        table.Rows.Add(1.5m, -2.5, true, DateTime.UtcNow, "note");

        return table;
    }

    private static DataSet ReadingsDataSet()
    {
        var dataSet = new DataSet("Readings");
        dataSet.Tables.Add(ReadingsTable());
        return dataSet;
    }

    private enum Status
    {
        Unknown = 0
    }

    private record Reading(decimal Amount, double Delta);
}
