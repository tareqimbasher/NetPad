using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace O2Html.Tests;

public class TypeNameHtmlTests
{
    [Fact]
    public void EscapesTheTupleHeader()
    {
        var html = HtmlSerializer.Serialize((1, "a")).ToHtml();

        Assert.Contains("ValueTuple&lt;Int32,&nbsp;String&gt;", html);
    }

    [Fact]
    public void TitlesANullableColumnWithAQuestionMark()
    {
        var html = HtmlSerializer.Serialize(new[] { new Line("A", 1) }).ToHtml();

        Assert.Contains("title=\"System.Int32?\"", html);
    }

    [Fact]
    public void ShowsTheKeyOfAGroupDumpedOnItsOwn()
    {
        var group = new[] { "a", "b" }.GroupBy(s => s.Length).First();

        var html = HtmlSerializer.Serialize(group).ToHtml();

        Assert.Contains("Key&nbsp;=&nbsp;1", html);
        Assert.Contains("IGrouping&lt;Int32,&nbsp;String&gt;", html);
    }

    [Fact]
    public void NamesAnArrayOfAnonymousTypes()
    {
        var html = HtmlSerializer.Serialize(new[] { new { Sku = "A", Qty = 1 } }).ToHtml();

        Assert.Contains("AnonymousType&lt;String,&nbsp;Int32&gt;[]", html);
    }

    public static TheoryData<object, string> TypeLinesAndTheirTitles => new()
    {
        { new List<int> { 1 }, "System.Collections.Generic.List<System.Int32>" },
        { (1, "a"), "System.ValueTuple<System.Int32, System.String>" },
        {
            new Dictionary<string, int> { ["a"] = 1 }.Keys,
            "System.Collections.Generic.Dictionary<System.String, System.Int32>.KeyCollection"
        },
        { new int?[] { 1, null }, "System.Int32?[]" },
        { new int[2, 3], "System.Int32[,]" },
    };

    [Theory]
    [MemberData(nameof(TypeLinesAndTheirTitles))]
    public void TitlesTheTypeLineWithTheFullName(object value, string expectedTitle)
    {
        Assert.Equal(expectedTitle, TypeLineTitle(value));
    }

    [Fact]
    public void TitlesTheTypeLineOfAWhereOverSimpleValuesWithTheIteratorClass()
    {
        var sequence = new List<int> { 1 }.Where(i => i > 0);
        var className = sequence.GetType().Name.Split('`')[0];

        var title = TypeLineTitle(sequence);

        Assert.StartsWith("System.Linq.", title);
        Assert.Contains("." + className + "<System.Int32>", title);
    }

    private static string TypeLineTitle(object value)
    {
        var html = HtmlSerializer.Serialize(value).ToHtml();
        var match = Regex.Match(html, "<tr class=\"table-info-header\" title=\"([^\"]*)\">");

        Assert.True(match.Success, $"No titled type line in: {html}");
        return match.Groups[1].Value;
    }

    private record Line(string Sku, int? Qty);
}
