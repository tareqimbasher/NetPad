using System.Collections.Generic;
using System.Linq;
using O2Html.Dom;

namespace O2Html.Tests;

/// <summary>
/// Helpers for asserting against a serialized DOM tree rather than against an HTML string, so that
/// assertions do not have to account for HTML escaping.
/// </summary>
internal static class TestHtml
{
    /// <summary>
    /// Serializes a value and returns every element in the result with the given tag name, in
    /// document order.
    /// </summary>
    public static IEnumerable<Element> Tags(object? value, string tagName, HtmlSerializerOptions? options = null)
    {
        return Descendants(HtmlSerializer.Serialize(value, options)).Where(e => e.TagName == tagName);
    }

    public static IEnumerable<Element> Descendants(Node node)
    {
        if (node is not Element element) yield break;

        foreach (var child in element.ChildElements)
        {
            yield return child;

            foreach (var descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// The element's own text, before HTML escaping and excluding the text of its children.
    /// </summary>
    public static string Text(this Element element)
    {
        return string.Concat(element.Children.OfType<TextNode>().Select(t => t.Text));
    }

    public static bool HasClass(this Element element, string cssClass) => element.ClassList.Contains(cssClass);
}
