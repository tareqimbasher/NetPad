using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetPad.Apps.Mcp.Tools;

internal static class JsonDefaults
{
    public static readonly JsonSerializerOptions CaseInsensitiveOptions = new() { PropertyNameCaseInsensitive = true };

    public static readonly JsonSerializerOptions IgnoreNullOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
