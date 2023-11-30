using O2Html.Dom;
using Xunit;

namespace O2Html.Tests.Dom;

public class ElementToHtmlTests
{
    [Fact]
    public void ToHtml_ElementWith1Child_ShouldGenerateCorrectHtml()
    {
        var element = new Element("div");
        element.AddElement("p");

        Assert.Equal("<div><p></p></div>", element.ToHtml());
    }

    [Fact]
    public void ToHtml_ElementWith1ChildAnd1SubChild_ShouldGenerateCorrectHtml()
    {
        var element = new Element("div");
        element.AddAndGetElement("p").AddAndGetElement("span").AddText("Test");

        Assert.Equal("<div><p><span>Test</span></p></div>", element.ToHtml());
    }

    [Fact]
    public void ToHtml_ElementWith1Child_WithNoFormatting_ShouldGenerateHtmlWithNoNewLines()
    {
        var element = new Element("div");
        element.AddElement("p");

        Assert.DoesNotContain("\n", element.ToHtml());
    }

    [Fact]
    public void ToHtml_ElementWith1ChildAnd1SubChild_WithNoFormatting_ShouldGenerateHtmlWithNoNewLines()
    {
        var element = new Element("div");
        element.AddAndGetElement("p").AddAndGetElement("span").AddText("Test");

        Assert.DoesNotContain("\n", element.ToHtml());
    }

    [Fact]
    public void ToHtml_ElementWith1Child_WithNewLinesFormatting_ShouldGenerateHtmlWithNewLines()
    {
        var element = new Element("div");
        element.AddElement("p");

        Assert.Equal("""
                     <div>
                       <p></p>
                     </div>
                     """, element.ToHtml(Formatting.Indented));
    }

    [Fact]
    public void ToHtml_ElementWith1ChildAnd1SubChild_WithNewLinesFormatting_ShouldGenerateHtmlWithNewLines()
    {
        var element = new Element("div");
        element.AddAndGetElement("p").AddAndGetElement("span").AddText("Test");

        Assert.Equal("""
                     <div>
                       <p>
                         <span>
                           Test
                         </span>
                       </p>
                     </div>
                     """, element.ToHtml(Formatting.Indented));
    }
}
