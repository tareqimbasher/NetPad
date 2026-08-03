using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetPad.Common;

/// <summary>
/// Writes an enum as a string, and reads an unrecognized string as the enum's default member
/// instead of throwing. Use it where the set of valid values can differ between the version that
/// wrote a file and the version reading it.
/// </summary>
public class TolerantJsonStringEnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            reader.Skip();
            return default;
        }

        return Enum.TryParse<TEnum>(reader.GetString(), true, out var value) ? value : default;
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
