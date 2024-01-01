using O2Html.Dom;
using O2Html.Dom.Elements;

namespace O2Html;

public static class HtmlExtensions
{
    /// <summary>
    /// Sets an attribute on this element and returns the same element.
    /// </summary>
    public static TElement SetAttribute<TElement>(this TElement element, string attributeName, string? value = null) where TElement : Element
    {
        element.GetOrAddAttribute(attributeName).Set(value);
        return element;
    }

    /// <summary>
    /// Deletes an attribute on this element and returns the same element.
    /// </summary>
    public static TElement DeleteAttribute<TElement>(this TElement element, string name) where TElement : Element
    {
        element.DeleteAndGetAttribute(name);
        return element;
    }

    /// <summary>
    /// Gets the value of the 'id' attribute of this element.
    /// </summary>
    public static string? Id<TElement>(this TElement element) where TElement : Element
    {
        return element.GetAttribute("id")?.Value;
    }

    /// <summary>
    /// Sets the value of the 'id' attribute on this element and returns the same element.
    /// </summary>
    public static TElement SetId<TElement>(this TElement element, string id) where TElement : Element
    {
        element.SetAttribute("id", id);
        return element;
    }

    /// <summary>
    /// Sets the value of the 'src' attribute on this element and returns the same element.
    /// </summary>
    public static TElement SetSrc<TElement>(this TElement element, string src) where TElement : Element
    {
        element.SetAttribute("src", src);
        return element;
    }

    /// <summary>
    /// Sets the value of the 'title' attribute on this element and returns the same element.
    /// </summary>
    public static TElement SetTitle<TElement>(this TElement element, string title) where TElement : Element
    {
        element.SetAttribute("title", title);
        return element;
    }


    /// <summary>
    /// Adds a child to this element and returns the element that was acted upon.
    /// </summary>
    public static TElement AddChild<TElement>(this TElement element, Node child) where TElement : Element
    {
        element.InternalAddChild(child);
        return element;
    }

    /// <summary>
    /// Adds a child to this element and returns the added child.
    /// </summary>
    public static TChild AddAndGetChild<TChild>(this Element element, TChild child) where TChild : Node
    {
        element.InternalAddChild(child);
        return child;
    }

    /// <summary>
    /// Inserts a child to this element at a specific index and returns the element that was acted upon.
    /// </summary>
    public static TElement InsertChild<TElement>(this TElement element, int index, Node child) where TElement : Element
    {
        element.InsertChild(index, child);
        return element;
    }

    /// <summary>
    /// Inserts a child to this element at a specific index and returns the added child.
    /// </summary>
    public static TChild InsertAndGetChild<TChild>(this Element element, int index, TChild child) where TChild : Node
    {
        element.InsertChild(index, child);
        return child;
    }

    /// <summary>
    /// Adds a child element to this element and returns the element that was acted upon.
    /// </summary>
    public static TElement AddElement<TElement>(this TElement element, string tagName) where TElement : Element
    {
        var child = new Element(tagName);
        element.InternalAddChild(child);
        return element;
    }

    /// <summary>
    /// Adds a child element to this element and returns the added child element.
    /// </summary>
    public static Element AddAndGetElement(this Element element, string tagName)
    {
        var child = new Element(tagName);
        element.InternalAddChild(child);
        return child;
    }

    /// <summary>
    /// Adds text to this element and returns the element that was acted upon.
    /// </summary>
    public static TElement AddText<TElement>(this TElement element, string? text) where TElement : Element
    {
        var child = new TextNode(text);
        element.InternalAddChild(child);
        return element;
    }

    /// <summary>
    /// Adds text to this element and returns the element the added child TextNode.
    /// </summary>
    public static TextNode AddAndGetText<TElement>(this TElement element, string? text) where TElement : Element
    {
        var child = new TextNode(text);
        element.InternalAddChild(child);
        return child;
    }

    /// <summary>
    /// Adds text to this element that will be escaped when converted to an HTML string and returns the element that was acted upon.
    /// ie. spaces will be converted to '&amp;nbsp;', &amp; will be converted to '&amp;amp;'...etc
    /// </summary>
    public static TElement AddEscapedText<TElement>(this TElement element, string? text) where TElement : Element
    {
        var child = new TextNode(text, true);
        element.InternalAddChild(child);
        return element;
    }

    /// <summary>
    /// Adds a <see cref="Null"/> child element to this element and returns the element that was acted upon.
    /// </summary>
    public static TElement AddNull<TElement>(this TElement element) where TElement : Element
    {
        element.AddAndGetNull();
        return element;
    }

    /// <summary>
    /// Adds a <see cref="Null"/> child element to this element and returns the added <see cref="Null"/> element.
    /// </summary>
    public static Null AddAndGetNull(this Element element)
    {
        var child = new Null();
        element.InternalAddChild(child);
        return child;
    }

    /// <summary>
    /// Adds a '&lt;br/&gt;' child element to this element and returns the element that was acted upon.
    /// </summary>
    public static TElement AddBreak<TElement>(this TElement element) where TElement : Element
    {
        return AddElement(element, "br");
    }

    /// <summary>
    /// Adds a '&lt;hr/&gt;' child element to this element and returns the element that was acted upon.
    /// </summary>
    public static TElement AddDivider<TElement>(this TElement element) where TElement : Element
    {
        return AddElement(element, "hr");
    }

    /// <summary>
    /// Adds a '&lt;script/&gt;' child element to this element and returns the element that was acted upon.
    /// </summary>
    public static TElement AddScript<TElement>(this TElement element, string code) where TElement : Element
    {
        element.AddAndGetScript(code);
        return element;
    }

    /// <summary>
    /// Adds a '&lt;script/&gt;' child element to this element and returns the added script element.
    /// </summary>
    public static Element AddAndGetScript(this Element element, string code)
    {
        var scriptEl = element.AddAndGetElement("script")
            .AddText(code);
        return scriptEl;
    }

    /// <summary>
    /// Adds a CSS class to this element and returns the element that was acted upon.
    /// </summary>
    public static TElement AddClass<TElement>(this TElement element, string className) where TElement : Element
    {
        element.ClassList.Add(className);
        return element;
    }

    /// <summary>
    /// Removes a CSS class on this element and returns the element that was acted upon.
    /// </summary>
    public static TElement RemoveClass<TElement>(this TElement element, string className) where TElement : Element
    {
        element.ClassList.Remove(className);
        return element;
    }
}
