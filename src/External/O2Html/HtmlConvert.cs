using O2Html.Dom;

namespace O2Html;

public static class HtmlConvert
{
    public static Element Serialize<T>(T? obj, HtmlSerializerSettings? htmlSerializerSettings = null)
    {
        var serializer = HtmlSerializer.Create(htmlSerializerSettings);
        return serializer.Serialize(obj);
    }
}
