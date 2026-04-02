using System.Text.Json;

namespace NetPad.Apps.Mcp.Tools;

internal static class JsonDefaults
{
    public static readonly JsonSerializerOptions IndentedOptions = new() { WriteIndented = true };

    public static readonly JsonSerializerOptions CaseInsensitiveOptions = new() { PropertyNameCaseInsensitive = true };
}
