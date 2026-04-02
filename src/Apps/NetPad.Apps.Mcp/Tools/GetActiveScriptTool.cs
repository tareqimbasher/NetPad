using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class GetActiveScriptTool
{
    [McpServerTool(Name = "get_active_script", ReadOnly = true), Description(
        "Get the currently active (focused) script in NetPad, including its code, configuration, and execution status.")]
    public static async Task<string> GetActiveScript(NetPadApiClient api, CancellationToken cancellationToken)
    {
        var activeId = await api.GetActiveScriptIdAsync(cancellationToken);

        if (activeId == null)
        {
            return "No active script.";
        }

        var env = await api.GetEnvironmentAsync(activeId.Value, cancellationToken);
        return JsonSerializer.Serialize(env, JsonDefaults.IndentedOptions);
    }
}
