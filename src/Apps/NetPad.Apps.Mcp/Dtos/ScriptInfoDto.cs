using System.Text.Json.Serialization;

namespace NetPad.Apps.Mcp.Dtos;

public class ScriptInfoDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = default!;

    [JsonPropertyName("targetFrameworkVersion")]
    public string TargetFrameworkVersion { get; set; } = default!;

    [JsonPropertyName("dataConnectionId")]
    public Guid? DataConnectionId { get; set; }

    [JsonPropertyName("isOpen")]
    public bool IsOpen { get; set; }

    [JsonPropertyName("isDirty")]
    public bool IsDirty { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("runDurationMilliseconds")]
    public double? RunDurationMilliseconds { get; set; }
}
