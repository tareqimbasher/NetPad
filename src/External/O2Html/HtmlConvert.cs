using O2Html.Dom;

namespace O2Html;

public static class HtmlConvert
{
    public static Node Serialize<T>(T? obj, HtmlSerializerSettings? htmlSerializerSettings = null)
    {
        var serializer = HtmlSerializer.Create(htmlSerializerSettings);
        var type = obj?.GetType() ?? typeof(T);
        return serializer.Serialize(obj, type);
    }
}
