using System.Text.Json.Serialization;

namespace NetPad.Apps.Mcp.Dtos;

public class HeadlessRunRequest
{
    [JsonPropertyName("code")]
    public required string Code { get; set; }

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "csharp";

    [JsonPropertyName("packages")]
    public PackageReferenceDto[]? Packages { get; set; }

    [JsonPropertyName("targetFramework")]
    public string? TargetFramework { get; set; }

    [JsonPropertyName("dataConnectionId")]
    public Guid? DataConnectionId { get; set; }

    [JsonPropertyName("timeoutMs")]
    public int? TimeoutMs { get; set; }
}

public class PackageReferenceDto
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }
}
