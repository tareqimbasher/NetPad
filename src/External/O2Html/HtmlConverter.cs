using System;
using O2Html.Dom;

namespace O2Html;

public abstract class HtmlConverter
{
    /// <summary>
    /// Converts an object to HTML.
    /// </summary>
    /// <typeparam name="T">Type of object being converted.</typeparam>
    /// <returns></returns>
    public abstract Node WriteHtml<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer);

    /// <summary>
    /// Converts an object to HTML assuming the object is being converted directly within a table row (a "tr" tag).
    /// Example:
    ///
    /// table
    ///   tbody
    ///     tr
    ///       # ASSUMES OBJECT IS BEING SERIALIZED HERE
    ///       # SO METHOD MUST ADD ITS OWN "td" TAGS
    ///     /tr
    ///   /tbody
    /// /table
    /// </summary>
    /// <typeparam name="T">Type of object being converted.</typeparam>
    public abstract void WriteHtmlWithinTableRow<T>(Element tr, T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer);

    /// <summary>
    /// Determines if this converter can convert the specified type.
    /// </summary>
    /// <returns>Returns true if this converter can convert the specified type; otherwise, false.</returns>
    public abstract bool CanConvert(HtmlSerializer htmlSerializer, Type type);
}
