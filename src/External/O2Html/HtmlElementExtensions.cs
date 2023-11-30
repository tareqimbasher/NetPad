using O2Html.Dom;
using O2Html.Dom.Elements;

namespace O2Html;

public static class HtmlElementExtensions
{
    public static TElement WithSetOrAddAttribute<TElement>(this TElement element, string attributeName) where TElement : Element
    {
        element.SetOrAddAttribute(attributeName, null);
        return element;
    }

    public static TElement WithSetOrAddAttribute<TElement>(this TElement element, string attributeName, string? value) where TElement : Element
    {
        element.SetOrAddAttribute(attributeName, value);
        return element;
    }

    public static TElement WithChild<TElement>(this TElement element, Node child) where TElement : Element
    {
        element.AddChild(child);
        return element;
    }

    public static TChild AddAndGetChild<TChild>(this Element element, TChild child) where TChild : Node
    {
        element.AddChild(child);
        return child;
    }

    public static TElement WithInsertChild<TElement>(this TElement element, int index, Node child) where TElement : Element
    {
        element.InsertChild(index, child);
        return element;
    }

    public static TChild InsertAndGetChild<TChild>(this Element element, int index, TChild child) where TChild : Node
    {
        element.InsertChild(index, child);
        return child;
    }

    public static TElement WithElement<TElement>(this TElement element, string tagName) where TElement : Element
    {
        element.AddElement(tagName);
        return element;
    }

    public static Element AddAndGetElement(this Element element, string tagName)
    {
        var child = new Element(tagName);
        element.AddChild(child);
        return child;
    }

    public static TElement WithText<TElement>(this TElement element, string? text) where TElement : Element
    {
        element.AddText(text);
        return element;
    }

    public static TextNode AddAndGetText(this Element element, string? text)
    {
        var child = new TextNode(text);
        element.AddChild(child);
        return child;
    }

    public static TElement WithNull<TElement>(this TElement element) where TElement : Element
    {
        element.AddAndGetNull();
        return element;
    }

    public static Null AddAndGetNull(this Element element)
    {
        var child = new Null();
        element.AddChild(child);
        return child;
    }


    public static TElement WithBreak<TElement>(this TElement element) where TElement : Element
    {
        return element.WithElement("<br/>");
    }

    public static TElement WithDivider<TElement>(this TElement element) where TElement : Element
    {
        return element.WithElement("<hr/>");
    }

    public static string? Id<TElement>(this TElement element) where TElement : Element
    {
        return element.GetAttribute("id")?.Value;
    }

    public static TElement WithId<TElement>(this TElement element, string id) where TElement : Element
    {
        element.SetOrAddAttribute("id", id);
        return element;
    }

    public static TElement WithAddClass<TElement>(this TElement element, string className) where TElement : Element
    {
        element.GetOrAddAttribute("class").Append(className, false);
        return element;
    }

    public static TElement WithRemoveClass<TElement>(this TElement element, string className) where TElement : Element
    {
        if (element.HasAttribute("class"))
            element.GetOrAddAttribute("class").Remove(className);
        return element;
    }

    public static TElement WithSrc<TElement>(this TElement element, string src) where TElement : Element
    {
        element.SetOrAddAttribute("src", src);
        return element;
    }

    public static TElement WithTitle<TElement>(this TElement element, string title) where TElement : Element
    {
        element.SetOrAddAttribute("title", title);
        return element;
    }
}
