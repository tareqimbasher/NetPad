using System.Text.Json.Serialization;

namespace NetPad.Apps.Mcp.Dtos;

public class ReferenceDto
{
    public const string PackageReferenceDiscriminator = "PackageReference";
    public const string AssemblyFileReferenceDiscriminator = "AssemblyFileReference";

    [JsonPropertyName("discriminator")]
    public string Discriminator { get; set; } = default!;

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("packageId")]
    public string? PackageId { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("assemblyPath")]
    public string? AssemblyPath { get; set; }
}
