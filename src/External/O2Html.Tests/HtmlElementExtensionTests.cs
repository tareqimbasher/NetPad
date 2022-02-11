using System.Linq;
using O2Html.Dom;
using Xunit;

namespace O2Html.Tests;

public class HtmlElementExtensionTests
{
    [Fact]
    public void AddAndGetElement_Adds_An_Element_To_Children_And_Returns_New_Element()
    {
        var element = new Element("div");
        var childElement = element.AddAndGetElement("p");

        Assert.Equal(typeof(Element), childElement.GetType());
        Assert.Equal(childElement, element.Children.Single());
    }

    [Fact]
    public void AddAndGetText_Adds_A_TextNode_To_Children_And_Returns_New_TextNode()
    {
        var element = new Element("div");
        var childTextNode = element.AddAndGetText("Test");

        Assert.Equal(typeof(TextNode), childTextNode.GetType());
        Assert.Equal(childTextNode, element.Children.Single());
    }
}
