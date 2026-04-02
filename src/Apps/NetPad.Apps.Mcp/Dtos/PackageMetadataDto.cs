using System.Text.Json.Serialization;

namespace NetPad.Apps.Mcp.Dtos;

public class PackageMetadataDto
{
    [JsonPropertyName("packageId")]
    public string PackageId { get; set; } = default!;

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = default!;

    [JsonPropertyName("authors")]
    public string? Authors { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("downloadCount")]
    public long? DownloadCount { get; set; }
}
