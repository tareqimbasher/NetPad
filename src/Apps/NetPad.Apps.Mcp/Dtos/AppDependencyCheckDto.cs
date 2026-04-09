using System.Text.Json.Serialization;

namespace NetPad.Apps.Mcp.Dtos;

public class AppDependencyCheckDto
{
    [JsonPropertyName("dotNetRuntimeVersion")]
    public string DotNetRuntimeVersion { get; set; } = default!;

    [JsonPropertyName("isSupportedDotNetEfToolInstalled")]
    public bool IsSupportedDotNetEfToolInstalled { get; set; }

    [JsonPropertyName("supportedDotNetSdkVersionsInstalled")]
    public DotNetSdkVersionDto[] SupportedDotNetSdkVersionsInstalled { get; set; } = [];
}

public class DotNetSdkVersionDto
{
    [JsonPropertyName("version")]
    public SdkVersionDto Version { get; set; } = default!;
}

public class SdkVersionDto
{
    [JsonPropertyName("major")]
    public int Major { get; set; }

    [JsonPropertyName("string")]
    public string String { get; set; } = default!;
}
