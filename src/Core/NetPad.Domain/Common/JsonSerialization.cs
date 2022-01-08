using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetPad.Common
{
    public static class JsonSerialization
    {
        static JsonSerialization()
        {
            DefaultOptions = new JsonSerializerOptions();
            Configure(DefaultOptions);
        }

        public static JsonSerializerOptions DefaultOptions { get; }

        public static JsonSerializerOptions Configure(JsonSerializerOptions options)
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }
    }
}
