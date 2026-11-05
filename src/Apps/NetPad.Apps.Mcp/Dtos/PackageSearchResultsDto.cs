using System.Text.Json.Serialization;

namespace NetPad.Apps.Mcp.Dtos;

public class PackageSearchResultsDto
{
    [JsonPropertyName("packages")]
    public PackageMetadataDto[] Packages { get; set; } = [];

    [JsonPropertyName("hasMorePages")]
    public bool HasMorePages { get; set; }

    [JsonPropertyName("unavailableSources")]
    public string[] UnavailableSources { get; set; } = [];
}
