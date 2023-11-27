using O2Html.Dom;
using Xunit;

namespace O2Html.Tests.Dom.Attributes;

public class ClassListTests
{
    private const string DefaultClassVal = "default-value";

    private Element ElementWithClassAttribute(bool setDefaultClassVal)
    {
        var element = new Element("div");
        var classAttribute = element.GetOrAddAttribute("class");

        if (setDefaultClassVal) classAttribute.Set(DefaultClassVal);

        Assert.Equal(!setDefaultClassVal, classAttribute.IsEmpty);

        return element;
    }

    [Fact]
    public void Add_AddsAttributeAndSetsValue_WhenAttributeDoesNotYetExist()
    {
        var element = new Element("div");
        Assert.Null(element.GetAttribute("class"));

        element.ClassList.Add("button");

        var classAttribute = element.GetAttribute("class");

        Assert.NotNull(classAttribute);
        Assert.Equal("button", classAttribute.Value);
    }

    [Fact]
    public void Add_AddsAttributeAndSetsValue_WhenAttributeAlreadyExistsAndIsEmpty()
    {
        var element = ElementWithClassAttribute(false);

        element.ClassList.Add("button");

        var classAttribute = element.GetAttribute("class");

        Assert.NotNull(classAttribute);
        Assert.Equal("button", classAttribute.Value);
    }

    [Fact]
    public void Add_AddsAttributeAndAppendsValue_WhenAttributeAlreadyExistsAndHasAValue()
    {
        var element = ElementWithClassAttribute(true);

        element.ClassList.Add("button");

        var classAttribute = element.GetAttribute("class");

        Assert.NotNull(classAttribute);
        Assert.Equal($"{DefaultClassVal} button", classAttribute.Value);
    }

    [Fact]
    public void Clear_ClearsClassAttributeValue()
    {
        var element = ElementWithClassAttribute(true);

        element.ClassList.Clear();

        var classAttribute = element.GetAttribute("class");

        Assert.NotNull(classAttribute);
        Assert.Empty(classAttribute.Value);
    }

    [Fact]
    public void Remove_RemovesItem_WhenOnlyOneExists()
    {
        var element = ElementWithClassAttribute(true);

        element.ClassList.Remove(DefaultClassVal);

        var classAttribute = element.GetAttribute("class");

        Assert.NotNull(classAttribute);
        Assert.Empty(classAttribute.Value);
    }

    [Fact]
    public void Remove_RemovesItem_WhenManyExist()
    {
        var element = ElementWithClassAttribute(true);

        element.ClassList.Add("button");
        element.ClassList.Remove(DefaultClassVal);

        var classAttribute = element.GetAttribute("class");

        Assert.NotNull(classAttribute);
        Assert.Equal("button", classAttribute.Value);
    }

    [Fact]
    public void Insert_InsertsCorrectly()
    {
        var element = ElementWithClassAttribute(false);
        var classAttribute = element.GetOrAddAttribute("class");

        classAttribute.Append("button1");
        classAttribute.Append("button2");
        classAttribute.Append("button3");

        element.ClassList.Insert(1, "inserted");

        Assert.Equal("button1 inserted button2 button3", classAttribute.Value);
    }

    [Fact]
    public void RemoveAt_RemovesCorrectly()
    {
        var element = ElementWithClassAttribute(false);
        var classAttribute = element.GetOrAddAttribute("class");

        classAttribute.Append("button1");
        classAttribute.Append("button2");
        classAttribute.Append("button3");

        element.ClassList.RemoveAt(1);

        Assert.Equal("button1 button3", classAttribute.Value);
    }

    [Fact]
    public void Indexer_GetsCorrectly()
    {
        var element = ElementWithClassAttribute(true);

        var val = element.ClassList[0];

        Assert.Equal(DefaultClassVal, val);
    }

    [Fact]
    public void Indexer_SetsCorrectly()
    {
        var element = ElementWithClassAttribute(false);
        var classAttribute = element.GetOrAddAttribute("class");

        classAttribute.Append("button1");
        classAttribute.Append("button2");
        classAttribute.Append("button3");

        element.ClassList[1] = "button-middle";

        Assert.Equal("button1 button-middle button3", classAttribute.Value);
    }
}
