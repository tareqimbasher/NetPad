using System.Text.Json.Serialization;

namespace NetPad.Apps.Mcp.Dtos;

public class HeadlessRunRequest
{
    public const string KindCSharp = "csharp";
    public const string KindSql = "sql";

    [JsonPropertyName("code")]
    public required string Code { get; set; }

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = KindCSharp;

    [JsonPropertyName("references")]
    public ReferenceDto[]? References { get; set; }

    [JsonPropertyName("targetFramework")]
    public string? TargetFramework { get; set; }

    [JsonPropertyName("dataConnectionId")]
    public Guid? DataConnectionId { get; set; }

    [JsonPropertyName("timeoutMs")]
    public int? TimeoutMs { get; set; }
}
