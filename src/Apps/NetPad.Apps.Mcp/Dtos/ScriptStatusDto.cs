using System.Text.Json.Serialization;

namespace NetPad.Apps.Mcp.Dtos;

public class ScriptStatusDto
{
    [JsonPropertyName("scriptId")]
    public Guid ScriptId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("status")]
    public string Status { get; set; } = default!;

    [JsonPropertyName("runDurationMs")]
    public double? RunDurationMs { get; set; }
}
