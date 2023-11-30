#if NETCOREAPP3_0_OR_GREATER
using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using O2Html.Common;
using O2Html.Dom;

namespace O2Html.Converters;

public class JsonDocumentHtmlConverter : DotNetTypeWithStringRepresentationHtmlConverter
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

    public override bool CanConvert(Type type)
    {
        return typeof(JsonDocument).IsAssignableFrom(type) ||
               typeof(JsonElement).IsAssignableFrom(type) ||
               typeof(JsonNode).IsAssignableFrom(type);
    }

    public override Node WriteHtml<T>(T obj, Type type, SerializationScope serializationScope, HtmlSerializer htmlSerializer)
    {
        string json;

        if (obj is JsonNode jsonNode)
        {
            json = jsonNode.ToJsonString(_jsonSerializerOptions);
        }
        else
        {
            JsonElement element;

            if (obj is JsonDocument jsonDocument)
            {
                element = jsonDocument.RootElement;
            }
            else if (obj is JsonElement el)
            {
                element = el;
            }
            else
            {
                throw new HtmlSerializationException(
                    $"The {nameof(JsonDocumentHtmlConverter)} can only convert objects of type {nameof(JsonDocument)} or {nameof(JsonElement)}");
            }

            json = element.ValueKind == JsonValueKind.Undefined ? "" : JsonSerializer.Serialize(element, _jsonSerializerOptions);
        }

        var pre = new Element("pre");
        pre.AddAndGetElement("code")
            .SetAttribute("language", "json")
            .AddText(Util.EscapeAngleBracketsForHtml(json));

        return pre;
    }
}
#endif
