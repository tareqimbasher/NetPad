using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class GetAppInfoTool
{
    [McpServerTool(Name = "get_app_info", ReadOnly = true), Description("Get information about the running NetPad instance, including version and dependency status.")]
    public static async Task<string> GetAppInfo(NetPadApiClient api, CancellationToken cancellationToken)
    {
        var identifierTask = api.GetAppIdentifierAsync(cancellationToken);
        var depsTask = api.CheckDependenciesAsync(cancellationToken);
        await Task.WhenAll(identifierTask, depsTask);

        var result = new
        {
            identifierTask.Result.Name,
            identifierTask.Result.Version,
            identifierTask.Result.ProductVersion,
            depsTask.Result.DotNetRuntimeVersion,
            depsTask.Result.IsSupportedDotNetEfToolInstalled
        };

        return JsonSerializer.Serialize(result, JsonDefaults.IndentedOptions);
    }
}
