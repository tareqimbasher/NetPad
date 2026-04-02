using System.Text.Json.Serialization;

namespace NetPad.Apps.Mcp.Dtos;

public class ScriptEnvironmentDto
{
    [JsonPropertyName("script")]
    public ScriptDto Script { get; set; } = default!;

    [JsonPropertyName("status")]
    public string Status { get; set; } = default!;

    [JsonPropertyName("runDurationMilliseconds")]
    public double? RunDurationMilliseconds { get; set; }
}
