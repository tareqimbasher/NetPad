using System;
using O2Html.Dom;

namespace O2Html;

public abstract class HtmlConverter
{
    public abstract Node WriteHtml<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer);

    public abstract void WriteHtmlWithinTableRow<T>(Element tr, T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer);

    public abstract bool CanConvert(HtmlSerializer htmlSerializer, Type type);
}
