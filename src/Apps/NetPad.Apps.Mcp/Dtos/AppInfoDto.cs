using System.Text.Json.Serialization;

namespace NetPad.Apps.Mcp.Dtos;

public class AppInfoDto
{
    [JsonPropertyName("identifier")]
    public AppIdentifierDto Identifier { get; set; } = default!;

    [JsonPropertyName("dependencyCheckResult")]
    public AppDependencyCheckDto DependencyCheckResult { get; set; } = default!;

    [JsonPropertyName("appDataDirectoryPath")]
    public string AppDataDirectoryPath { get; set; } = default!;

    [JsonPropertyName("osDescription")]
    public string OsDescription { get; set; } = default!;

    [JsonPropertyName("osArchitecture")]
    public string OsArchitecture { get; set; } = default!;
}
