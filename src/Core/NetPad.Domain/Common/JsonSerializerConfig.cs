using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetPad.Common
{
    public static class JsonSerializerConfig
    {
        static JsonSerializerConfig()
        {
            DefaultJsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters =
                {
                    new JsonStringEnumConverter()
                }
            };
        }

        public static JsonSerializerOptions DefaultJsonSerializerOptions { get; }
    }
}
