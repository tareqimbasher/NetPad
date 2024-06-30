using System;
using O2Html.Dom;

namespace O2Html;

/// <summary>
/// Converts .NET objects or value types to a HTML DOM tree.
/// </summary>
public abstract class HtmlConverter
{
    /// <summary>
    /// Determines if this converter can convert the specified type to HTML.
    /// </summary>
    /// <returns>Returns true if this converter can convert the specified type; otherwise, false.</returns>
    public abstract bool CanConvert(Type type);

    /// <summary>
    /// Converts an object to HTML.
    /// </summary>
    /// <typeparam name="T">Type of object being converted.</typeparam>
    /// <returns></returns>
    public abstract Node WriteHtml<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer);

    /// <summary>
    /// Converts an object to HTML assuming the object is being converted directly within a table row (a "tr" tag).
    /// </summary>
    /// <example>
    /// <code>
    /// table
    ///   tbody
    ///     tr
    ///       # ASSUMES OBJECT IS BEING SERIALIZED HERE
    ///       # SO METHOD MUST ADD ITS OWN "td" TAGS
    ///     /tr
    ///   /tbody>
    /// </code>
    /// /table
    /// </example>
    /// <typeparam name="T">Type of object being converted.</typeparam>
    public abstract void WriteHtmlWithinTableRow<T>(Element tr, T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer);
}
