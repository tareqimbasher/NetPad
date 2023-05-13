using System;
using System.Diagnostics.CodeAnalysis;
using O2Html.Dom;
using O2Html.Dom.Elements;

namespace O2Html;

public abstract class HtmlConverter
{
    /// <summary>
    /// Determines if this converter can convert the specified type to HTML.
    /// </summary>
    /// <returns>Returns true if this converter can convert the specified type; otherwise, false.</returns>
    public abstract bool CanConvert(HtmlSerializer htmlSerializer, Type type);

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

    protected virtual bool ShouldShortCircuit<T>(
        T obj,
        Type type,
        SerializationScope serializationScope,
        HtmlSerializer htmlSerializer,
#if NETSTANDARD2_1 || NETCOREAPP3_0_OR_GREATER
        [NotNullWhen(true)]
#endif
        out Node? shortCircuitValue)
    {
        shortCircuitValue = null;

        if (!serializationScope.CheckAlreadySerializedOrAdd(obj)) return false;

        var referenceLoopHandling = htmlSerializer.SerializerSettings.ReferenceLoopHandling;

        if (referenceLoopHandling == ReferenceLoopHandling.IgnoreAndSerializeCyclicReference)
            shortCircuitValue = new CyclicReference(type).WithAddClass(htmlSerializer.SerializerSettings.CssClasses.CyclicReference);

        else if (referenceLoopHandling == ReferenceLoopHandling.Ignore)
            shortCircuitValue = new Element("div");

        else if (referenceLoopHandling == ReferenceLoopHandling.Error)
            throw new HtmlSerializationException($"A reference loop was detected. Object already serialized: {type.FullName}");

        return shortCircuitValue != null;
    }
}
