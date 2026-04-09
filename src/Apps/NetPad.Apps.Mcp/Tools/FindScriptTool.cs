using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class FindScriptTool
{
    [McpServerTool(Name = "find_script", ReadOnly = true, Destructive = false, Idempotent = true), Description(
        "Find scripts by name using case-insensitive partial matching. " +
        "Searches both open and saved scripts. " +
        "Returns matching scripts with their ID, name, path, and status.")]
    public static async Task<string> FindScript(
        NetPadApiClient api,
        [Description("Name or partial name to search for")] string name,
        CancellationToken cancellationToken)
    {
        var scripts = await api.GetScriptsInfoAsync(name, cancellationToken);

        if (scripts.Length == 0)
        {
            return $"No scripts found matching '{name}'.";
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
