using System.Text.Json.Serialization;

namespace NetPad.Apps.Mcp.Dtos;

public class AppIdentifierDto
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("productVersion")]
    public string? ProductVersion { get; set; }
}
