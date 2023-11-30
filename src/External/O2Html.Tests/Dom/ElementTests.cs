using System.Linq;
using O2Html.Dom;
using Xunit;

namespace O2Html.Tests.Dom;

public class ElementTests
{
    [Fact]
    public void TagName_Is_Cleaned_From_Xml_Node_Separators()
    {
        var element = new Element("<div/>");
        Assert.Equal("div", element.TagName);
    }

    [Fact]
    public void TagName_Casing_IsNormalized()
    {
        var element = new Element("dIv");
        Assert.Equal("div", element.TagName);
    }

    [Fact]
    public void Considered_IsSelfClosing_If_TagName_Ends_With_Angle_Bracket()
    {
        var element = new Element("<div/>");
        Assert.True(element.IsSelfClosing);
    }

    [Fact]
    public void Not_Considered_IsSelfClosing_If_TagName_Does_Not_End_With_Angle_Bracket()
    {
        var element = new Element("<div");
        Assert.False(element.IsSelfClosing);
    }

    [Fact]
    public void Attributes_Are_Empty_On_Initialization()
    {
        Assert.Empty(new Element("div").Attributes);
    }

    [Fact]
    public void Children_Are_Empty_On_Initialization()
    {
        Assert.Empty(new Element("div").Children);
    }

    [Fact]
    public void ChildElements_Returns_Children_Of_Type_Element()
    {
        var element = new Element("div");
        var childNode = new TextNode("Test");
        element.AddChild(childNode);

        var childElement = new Element("p");
        element.AddChild(childElement);

        Assert.Equal(childElement, element.ChildElements.Single());
    }

    [Fact]
    public void AddChild_Adds_Node_To_Children()
    {
        var element = new Element("div");
        var childNode = new TextNode("Test");
        element.AddChild(childNode);

        Assert.Equal(childNode, element.Children.Single());
    }

    [Fact]
    public void RemoveChild_Removes_Node_From_Children()
    {
        var element = new Element("div");
        var childNode = new TextNode("Test");
        element.AddChild(childNode);
        element.RemoveChild(childNode);

        Assert.Empty(element.Children);
    }

    [Fact]
    public void HasAttribute_Returns_If_Element_Has_Attribute()
    {
        var element = new Element("div");
        element.Attributes.Add(new ElementAttribute(element, "test"));

        Assert.True(element.HasAttribute("test"));
    }

    [Fact]
    public void GetAttribute_Returns_Attribute_When_It_Exists()
    {
        var element = new Element("div");
        var attribute = new ElementAttribute(element, "test");
        element.Attributes.Add(attribute);

        Assert.Equal(attribute, element.GetAttribute("test"));
    }

    [Fact]
    public void GetAttribute_Returns_Null_When_Attribute_Does_Not_Exist()
    {
        var element = new Element("div");

        Assert.Null(element.GetAttribute("test"));
    }

    [Fact]
    public void GetOrAddAttribute_Returns_Attribute_When_It_Exists()
    {
        var element = new Element("div");
        var attribute = new ElementAttribute(element, "test");
        element.Attributes.Add(attribute);

        Assert.Equal(attribute, element.GetOrAddAttribute("test"));
    }

    [Fact]
    public void GetOrAddAttribute_Adds_And_Returns_Attribute_When_It_Does_Not_Exist()
    {
        var element = new Element("div");
        var attribute = element.GetOrAddAttribute("test");

        Assert.Equal(attribute, element.GetAttribute("test"));
    }

    [Fact]
    public void GetOrAddAttribute_Does_Not_Add_Duplicates()
    {
        var element = new Element("div");
        element.GetOrAddAttribute("test");
        element.GetOrAddAttribute("test");

        Assert.Single(element.Attributes.Where(a => a.Name == "test"));
    }

    [Fact]
    public void SetOrAddAttribute_Returns_Attribute_And_Sets_Value_When_It_Exist()
    {
        var element = new Element("div");
        var attribute = new ElementAttribute(element, "test");
        element.Attributes.Add(attribute);

        element.SetAttribute("test", "val");

        Assert.Equal(attribute, element.GetAttribute("test"));
        Assert.Equal("val", attribute.Value);
    }

    [Fact]
    public void SetOrAddAttribute_Adds_And_Returns_Attribute_And_Sets_Value_When_It_Does_Not_Exist()
    {
        var element = new Element("div");
        var attribute = element.GetOrAddAttribute("test").Set("val");

        Assert.Equal(attribute, element.GetOrAddAttribute("test"));
        Assert.Equal("val", attribute.Value);
    }

    [Fact]
    public void SetOrAddAttribute_Does_Not_Add_Duplicates()
    {
        var element = new Element("div");
        element.SetAttribute("test", "val");
        element.SetAttribute("test", "val");

        Assert.Single(element.Attributes.Where(a => a.Name == "test"));
    }

    [Fact]
    public void DeleteAttribute_By_Name_Removes_Attribute()
    {
        var element = new Element("div");
        var attribute = element.GetOrAddAttribute("test");

        element.DeleteAttribute(attribute.Name);

        Assert.Empty(element.Attributes.Where(a => a.Name == attribute.Name));
    }

    [Fact]
    public void Attribute_Delete_Removes_Attribute()
    {
        var element = new Element("div");
        var attribute = element.GetOrAddAttribute("test");

        attribute.Delete();

        Assert.DoesNotContain(attribute, element.Attributes);
    }

    [Fact]
    public void HtmlTest_Empty_Not_SelfClosing()
    {
        var element = new Element("div");

        Assert.Equal("<div></div>", element.ToHtml());
    }

    [Fact]
    public void HtmlTest_Empty_SelfClosing()
    {
        var element = new Element("<br/>");

        Assert.Equal("<br/>", element.ToHtml());
    }

    [Fact]
    public void HtmlTest_With_Attributes()
    {
        var element = new Element("div");
        element.SetAttribute("id", "test");
        element.SetAttribute("style", "width: 100%");

        Assert.Equal("<div id=\"test\" style=\"width: 100%\"></div>", element.ToHtml());
    }
}
