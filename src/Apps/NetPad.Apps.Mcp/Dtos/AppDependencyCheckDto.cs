using System.Text.Json.Serialization;

namespace NetPad.Apps.Mcp.Dtos;

public class AppDependencyCheckDto
{
    [JsonPropertyName("dotNetRuntimeVersion")]
    public string DotNetRuntimeVersion { get; set; } = default!;

    [JsonPropertyName("isSupportedDotNetEfToolInstalled")]
    public bool IsSupportedDotNetEfToolInstalled { get; set; }
}
