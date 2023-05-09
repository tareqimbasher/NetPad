using O2Html.Dom;

namespace O2Html;

/// <summary>
/// Provides methods for converting .NET types to HTML.
/// </summary>
public static class HtmlConvert
{
    /// <summary>
    /// Serializes an object to a HTML DOM tree.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <param name="htmlSerializerSettings">Serialization settings.</param>
    /// <typeparam name="T">Type of object being serialized.</typeparam>
    /// <returns>HTML DOM tree representing the object being serialized.</returns>
    public static Node Serialize<T>(T? obj, HtmlSerializerSettings? htmlSerializerSettings = null)
    {
        var serializer = HtmlSerializer.Create(htmlSerializerSettings);
        var type = obj?.GetType() ?? typeof(T);
        return serializer.Serialize(obj, type);
    }
}
