using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetPad.Apps.Mcp.Dtos;

public class HeadlessRunResult
{
    public const string StatusCompleted = "completed";
    public const string StatusFailed = "failed";
    public const string StatusTimeout = "timeout";
    public const string StatusCancelled = "cancelled";
    public const string StatusPending = "pending";

    [JsonPropertyName("status")]
    public string Status { get; set; } = default!;

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("durationMs")]
    public double DurationMs { get; set; }

    [JsonPropertyName("output")]
    public List<JsonElement> Output { get; set; } = [];

    [JsonPropertyName("compilationErrors")]
    public List<string>? CompilationErrors { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
