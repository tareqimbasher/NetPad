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
        var identifierTask = api.GetAppIdentifierAsync(cancellationToken);
        var depsTask = api.CheckDependenciesAsync(cancellationToken);
        await Task.WhenAll(identifierTask, depsTask);

        var deps = depsTask.Result;

        var supportedTargetFrameworks = deps.SupportedDotNetSdkVersionsInstalled
            .Select(sdk => $"DotNet{sdk.Version.Major}")
            .Distinct()
            .ToArray();

        var installedDotNetSdks = deps.SupportedDotNetSdkVersionsInstalled
            .Select(sdk => sdk.Version.String)
            .ToArray();

        var result = new
        {
            identifierTask.Result.Name,
            identifierTask.Result.Version,
            identifierTask.Result.ProductVersion,
            deps.DotNetRuntimeVersion,
            deps.IsSupportedDotNetEfToolInstalled,
            SupportedTargetFrameworks = supportedTargetFrameworks,
            InstalledDotNetSdks = installedDotNetSdks
        };

        return JsonSerializer.Serialize(result);
    }
}
