using System.Text.Json;

namespace NetPad.Common
{
    public static class JsonSerializerConfig
    {
        static JsonSerializerConfig()
        {
            DefaultJsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public static JsonSerializerOptions DefaultJsonSerializerOptions { get; }
    }
}
