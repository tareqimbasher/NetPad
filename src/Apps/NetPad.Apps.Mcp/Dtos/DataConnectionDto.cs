using System.Text.Json.Serialization;

namespace NetPad.Apps.Mcp.Dtos;

public class DataConnectionDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("type")]
    public string Type { get; set; } = default!;

    [JsonPropertyName("host")]
    public string? Host { get; set; }

    [JsonPropertyName("port")]
    public string? Port { get; set; }

    [JsonPropertyName("databaseName")]
    public string? DatabaseName { get; set; }
}
