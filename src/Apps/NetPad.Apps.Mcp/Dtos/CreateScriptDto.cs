using System.Text.Json.Serialization;

namespace NetPad.Apps.Mcp.Dtos;

public class CreateScriptDto
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("dataConnectionId")]
    public Guid? DataConnectionId { get; set; }

    [JsonPropertyName("runImmediately")]
    public bool RunImmediately { get; set; }
}
