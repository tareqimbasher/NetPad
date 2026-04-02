using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class FindScriptTool
{
    [McpServerTool(Name = "find_script", ReadOnly = true), Description(
        "Find scripts by name using case-insensitive partial matching. " +
        "Returns matching scripts with their ID, name, and path.")]
    public static async Task<string> FindScript(
        NetPadApiClient api,
        [Description("Name or partial name to search for")] string name,
        CancellationToken cancellationToken)
    {
        var scripts = await api.FindScriptsAsync(name, cancellationToken);

        if (scripts.Length == 0)
        {
            return $"No scripts found matching '{name}'.";
        }

        return JsonSerializer.Serialize(scripts);
    }
}
