using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetPad.Common;

public class VersionJsonConverter : JsonConverter<Version>
{
    public override Version? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException("Cannot convert null or whitespace to Version");
        }

        return Version.TryParse(value, out var version)
            ? version
            : throw new JsonException($"Could not convert value to Version: {value.Truncate(10, true)}");
    }

    public override void Write(Utf8JsonWriter writer, Version value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName("major");
        writer.WriteNumberValue(value.Major);

        writer.WritePropertyName("minor");
        writer.WriteNumberValue(value.Minor);

        writer.WritePropertyName("build");
        writer.WriteNumberValue(value.Build);

        writer.WritePropertyName("revision");
        writer.WriteNumberValue(value.Revision);

        writer.WritePropertyName("majorRevision");
        writer.WriteNumberValue(value.MajorRevision);

        writer.WritePropertyName("minorRevision");
        writer.WriteNumberValue(value.MinorRevision);

        writer.WriteEndObject();
    }
}
