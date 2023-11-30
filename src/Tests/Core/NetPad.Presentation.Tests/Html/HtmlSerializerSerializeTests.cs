using NetPad.Presentation.Html;

namespace NetPad.Presentation.Tests.Html;

public class HtmlSerializerSerializeTests
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

        var element = HtmlPresenter.SerializeToElement(output, new DumpOptions(AppendNewLine: true));

        Assert.Equal("br", element.ChildElements.Last().TagName, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void AppendsHtmlLineBreak_WhenSpecifiedEvenIfTitled()
    {
        var output = GetAllTextOutput();

        var element = HtmlPresenter.SerializeToElement(output, new DumpOptions(Title: "some title", AppendNewLine: true));

        Assert.Equal("br", element.ChildElements.Last().TagName, StringComparer.OrdinalIgnoreCase);
    }
}
