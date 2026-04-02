using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class GetScriptStatusTool
{
    [McpServerTool(Name = "get_script_status", ReadOnly = true), Description(
        "Get the execution status of an open script. Status is one of: Ready, Running, Stopping, Error.")]
    public static async Task<string> GetScriptStatus(
        NetPadApiClient api,
        [Description("Script ID (GUID)")] string scriptId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(scriptId, out var id))
            return "Invalid scriptId format. Expected a GUID.";

        var status = await api.GetEnvironmentStatusAsync(id, cancellationToken);
        return JsonSerializer.Serialize(status);
    }
}
