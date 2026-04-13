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

    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    [JsonPropertyName("targetFrameworkVersion")]
    public string? TargetFrameworkVersion { get; set; }

    [JsonPropertyName("optimizationLevel")]
    public string? OptimizationLevel { get; set; }

    [JsonPropertyName("useAspNet")]
    public bool? UseAspNet { get; set; }

    [JsonPropertyName("namespaces")]
    public string[]? Namespaces { get; set; }

    [JsonPropertyName("references")]
    public ReferenceDto[]? References { get; set; }
}
