using System.Text.Json;

namespace NetPad.Utilities
{
    public class JsonSerialization
    {
        public static JsonSerializerOptions DefaultJsonSerializerOptions { get; }
            = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
    }
}
