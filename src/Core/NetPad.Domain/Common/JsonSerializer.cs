using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetPad.Common
{
    public static class JsonSerializer
    {
        static JsonSerializer()
        {
            DefaultOptions = Configure(new JsonSerializerOptions());
        }

        public static JsonSerializerOptions DefaultOptions { get; }

        public static JsonSerializerOptions Configure(JsonSerializerOptions options)
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }

        public static string Serialize(object? value, bool indented = false)
        {
            return indented
                ? System.Text.Json.JsonSerializer.Serialize(value, Configure(new JsonSerializerOptions { WriteIndented = true }))
                : System.Text.Json.JsonSerializer.Serialize(value, DefaultOptions);
        }

        public static T? Deserialize<T>(string json)
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(json, DefaultOptions);
        }
    }
}
