using System.ComponentModel;
using System.Text.Json.Serialization;

namespace NetPad.Apps.Mcp.Dtos;

/// <summary>
/// Structured input for NuGet package references, used across MCP tool parameters.
/// </summary>
public class PackageInput
{
    [Description("NuGet package ID (e.g. 'Newtonsoft.Json')")]
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [Description("Package version (e.g. '13.0.3'). If omitted, latest stable version is used automatically.")]
    [JsonPropertyName("version")]
    public string? Version { get; set; }
}
