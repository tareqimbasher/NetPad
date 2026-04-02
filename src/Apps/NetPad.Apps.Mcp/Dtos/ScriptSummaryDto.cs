using System.Text.Json.Serialization;

namespace NetPad.Apps.Mcp.Dtos;

public class ScriptSummaryDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = default!;
}
