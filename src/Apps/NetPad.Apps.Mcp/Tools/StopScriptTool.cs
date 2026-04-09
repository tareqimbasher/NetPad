using System.ComponentModel;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class StopScriptTool
{
    [McpServerTool(Name = "stop_script", Destructive = false, Idempotent = true), Description(
        "Stop a running script. If the script is not currently running, this has no effect.")]
    public static async Task<string> StopScript(
        NetPadApiClient api,
        [Description("Script ID (GUID)")] string scriptId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(scriptId, out var id))
            return "Invalid scriptId format. Expected a GUID.";

        await api.StopScriptAsync(id, cancellationToken);

        return "Script stop requested.";
    }
}
