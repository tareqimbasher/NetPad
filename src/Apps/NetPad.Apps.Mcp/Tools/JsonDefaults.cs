using System.Text.Json;

namespace NetPad.Apps.Mcp.Tools;

internal static class JsonDefaults
{
    public static readonly JsonSerializerOptions CaseInsensitiveOptions = new() { PropertyNameCaseInsensitive = true };
}
