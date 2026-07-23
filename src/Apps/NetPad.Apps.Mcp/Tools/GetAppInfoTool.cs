using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class GetAppInfoTool
{
    [McpServerTool(Name = "get_app_info", ReadOnly = true, Destructive = false, Idempotent = true),
     Description(
         "Get information about the running NetPad instance, including version, dependency status, " +
         "and supported target frameworks.")]
    public static async Task<string> GetAppInfo(NetPadApiClient api, CancellationToken cancellationToken)
    {
        var info = await api.GetAppInfoAsync(cancellationToken);
        var identifier = info.Identifier;
        var deps = info.DependencyCheckResult;

        var supportedTargetFrameworks = deps.SupportedDotNetSdkVersionsInstalled
            .Select(sdk => $"DotNet{sdk.Version.Major}")
            .Distinct()
            .ToArray();

        var installedDotNetSdks = deps.SupportedDotNetSdkVersionsInstalled
            .Select(sdk => sdk.Version.String)
            .ToArray();

        var result = new
        {
            identifier.Name,
            identifier.Version,
            identifier.ProductVersion,
            deps.DotNetRuntimeVersion,
            deps.IsSupportedDotNetEfToolInstalled,
            SupportedTargetFrameworks = supportedTargetFrameworks,
            InstalledDotNetSdks = installedDotNetSdks,
            info.AppDataDirectoryPath,
            info.OsDescription,
            info.OsArchitecture
        };

        return JsonSerializer.Serialize(result);
    }
}
