using System.Text.Json.Serialization;

namespace NetPad.Apps.Mcp.Dtos;

public class DataConnectionTestResultDto
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
