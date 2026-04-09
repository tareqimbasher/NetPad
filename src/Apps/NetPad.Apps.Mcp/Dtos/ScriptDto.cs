using System.Text.Json.Serialization;

namespace NetPad.Apps.Mcp.Dtos;

public class ScriptDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("path")]
    public string? Path { get; set; }

    [JsonPropertyName("isNew")]
    public bool IsNew { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("isDirty")]
    public bool IsDirty { get; set; }

    [JsonPropertyName("config")]
    public ScriptConfigDto? Config { get; set; }

    [JsonPropertyName("dataConnection")]
    public DataConnectionDto? DataConnection { get; set; }
}

public class ScriptConfigDto
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = default!;

    [JsonPropertyName("targetFrameworkVersion")]
    public string TargetFrameworkVersion { get; set; } = default!;

    [JsonPropertyName("optimizationLevel")]
    public string OptimizationLevel { get; set; } = default!;

    [JsonPropertyName("useAspNet")]
    public bool UseAspNet { get; set; }

    [JsonPropertyName("namespaces")]
    public string[] Namespaces { get; set; } = [];
}
