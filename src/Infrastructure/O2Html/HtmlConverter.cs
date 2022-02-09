using System;
using O2Html.Dom;

namespace O2Html;

public abstract class HtmlConverter
{
    public abstract Element WriteHtml<T>(T obj, SerializationScope serializationScope, HtmlSerializer htmlSerializer);

    public abstract void WriteHtmlWithinTableRow<T>(Element tr, T obj, SerializationScope serializationScope, HtmlSerializer htmlSerializer);

    public abstract bool CanConvert(Type type);
}
