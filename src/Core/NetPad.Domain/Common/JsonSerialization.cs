using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetPad.Common
{
    public static class JsonSerialization
    {
        static JsonSerialization()
        {
            DefaultOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters =
                {
                    new JsonStringEnumConverter()
                }
            };
        }

        public static JsonSerializerOptions DefaultOptions { get; }
    }
}
