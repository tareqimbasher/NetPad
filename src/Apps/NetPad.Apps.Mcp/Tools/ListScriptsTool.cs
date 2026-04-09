using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class ListScriptsTool
{
    [McpServerTool(Name = "list_scripts", ReadOnly = true, Destructive = false, Idempotent = true), Description(
        "List all scripts — both currently open in NetPad and saved on disk. " +
        "Open scripts include their execution status.")]
    public static async Task<string> ListScripts(NetPadApiClient api, CancellationToken cancellationToken)
    {
        var scripts = await api.GetScriptsInfoAsync(cancellationToken: cancellationToken);

        if (scripts.Length == 0)
        {
            return "No scripts found.";
        }

        var result = scripts.Select(s => new
        {
            s.Id,
            s.Name,
            s.Path,
            s.IsOpen,
            s.Status,
            s.RunDurationMilliseconds
        });

        return JsonSerializer.Serialize(result);
    }
}
