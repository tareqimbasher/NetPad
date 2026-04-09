using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class GetPackageVersionsTool
{
    [McpServerTool(Name = "get_package_versions", ReadOnly = true, Destructive = false, Idempotent = true), Description(
        "Get all available versions of a NuGet package. Use search_packages to find package IDs if needed.")]
    public static async Task<string> GetPackageVersions(
        NetPadApiClient api,
        [Description("NuGet package ID (e.g. 'Newtonsoft.Json')")] string packageId,
        [Description("Whether to include pre-release versions")] bool includePrerelease = false,
        CancellationToken cancellationToken = default)
    {
        var versions = await api.GetPackageVersionsAsync(packageId, includePrerelease, cancellationToken);
        return JsonSerializer.Serialize(versions);
    }
}
