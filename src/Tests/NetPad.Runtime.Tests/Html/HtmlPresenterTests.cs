using NetPad.Presentation;
using NetPad.Presentation.Html;
using O2Html.Dom;

namespace NetPad.Runtime.Tests.Html;

public class HtmlPresenterTests
{
    private static object GetGenericOutput() => new
    {
        Name = "John",
        Age = 30
    };

    private static string GetAllTextOutput() => "text output";

    [Fact]
    public void OutputShouldBeWrappedInAGroup()
    {
        var output = GetGenericOutput();

        var element = HtmlPresenter.SerializeToElement(output);

        Assert.Contains("group", element.ClassList);
    }

    [Fact]
    public void GroupShouldHaveErrorClass_WhenSerializingErrors()
    {
        var output = GetGenericOutput();

        var element = HtmlPresenter.SerializeToElement(output, isError: true);

        Assert.Contains("error", element.ClassList);
    }

    [Fact]
    public void GroupShouldHaveErrorClass_WhenSerializingExceptions()
    {
        var element = HtmlPresenter.SerializeToElement(new Exception());

        Assert.Contains("error", element.ClassList);
    }

    [Fact]
    public void GroupShouldHaveTitledClass_WhenOutputHasTitle()
    {
        var output = GetGenericOutput();

        var element = HtmlPresenter.SerializeToElement(output, new DumpOptions(Title: "some title"));

        Assert.Contains("titled", element.ClassList);
    }

    [Fact]
    public void GroupShouldHaveTitledClass_WhenOutputHasTitleEvenIfError()
    {
        var element = HtmlPresenter.SerializeToElement(new Exception(), new DumpOptions(Title: "some title"));

        Assert.Contains("titled", element.ClassList);
        Assert.Contains("error", element.ClassList);
    }

    [Fact]
    public void GroupShouldHaveTextClass_WhenOutputIsAllText()
    {
        var output = GetAllTextOutput();

        var element = HtmlPresenter.SerializeToElement(output);

        Assert.Contains("text", element.ClassList);
    }

    [Fact]
    public void GroupShouldNotHaveTextClass_WhenOutputIsNotAllText()
    {
        var output = GetGenericOutput();

        var element = HtmlPresenter.SerializeToElement(output);

        Assert.DoesNotContain("text", element.ClassList);
    }

    [Fact]
    public void AppendsHtmlLineBreak_WhenSpecified()
    {
        var output = GetAllTextOutput();

        var element = HtmlPresenter.SerializeToElement(output, new DumpOptions(AppendNewLineToAllTextOutput: true));

        Assert.Equal("br", element.ChildElements.Last().TagName, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void AppendsHtmlLineBreak_WhenSpecifiedEvenIfTitled()
    {
        var output = GetAllTextOutput();

        var element = HtmlPresenter.SerializeToElement(output, new DumpOptions(Title: "some title", AppendNewLineToAllTextOutput: true));

        Assert.Equal("br", element.ChildElements.Last().TagName, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void NumericColumns_AreMarkedForBarGraphs()
    {
        var output = new[]
        {
            new { Name = "John", Age = 30, Score = 98.5m },
            new { Name = "Jane", Age = 25, Score = 72.25m }
        };

        var element = HtmlPresenter.SerializeToElement(output);

        var headings = FindDescendants(element, "th")
            .Where(x => x.Parent?.Parent is Element { TagName: "thead" })
            .ToArray();

        Assert.Equal("true", headings.Single(x => x.Children.OfType<TextNode>().Any(t => t.Text == "Age"))
            .GetAttribute("data-bar-graph")?.Value);
        Assert.Equal("true", headings.Single(x => x.Children.OfType<TextNode>().Any(t => t.Text == "Score"))
            .GetAttribute("data-bar-graph")?.Value);
        Assert.Null(headings.Single(x => x.Children.OfType<TextNode>().Any(t => t.Text == "Name"))
            .GetAttribute("data-bar-graph"));
    }

    [Fact]
    public void NumericCells_IncludeInvariantBarGraphValues()
    {
        var output = new[]
        {
            new { Name = "John", Age = 30, Score = 98.5m }
        };

        var element = HtmlPresenter.SerializeToElement(output);

        var cells = FindDescendants(element, "td").ToArray();

        Assert.Equal("30", cells.Single(x => x.GetAttribute("data-bar-graph-value")?.Value == "30")
            .GetAttribute("data-bar-graph-value")?.Value);
        Assert.Equal("98.5", cells.Single(x => x.GetAttribute("data-bar-graph-value")?.Value == "98.5")
            .GetAttribute("data-bar-graph-value")?.Value);
    }

    private static IEnumerable<Element> FindDescendants(Element root, string tagName)
    {
        foreach (var child in root.ChildElements)
        {
            if (string.Equals(child.TagName, tagName, StringComparison.OrdinalIgnoreCase))
            {
                yield return child;
            }

            foreach (var descendant in FindDescendants(child, tagName))
            {
                yield return descendant;
            }
        }
    }
}
