using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class OpenScriptTool
{
    [McpServerTool(Name = "open_script", Destructive = false, Idempotent = true), Description(
        "Open a script in the NetPad editor. The script can be identified by its ID or file path. " +
        "If the script is already open, it will be activated and its current environment returned. " +
        "Either scriptId or path must be provided.")]
    public static async Task<string> OpenScript(
        NetPadApiClient api,
        [Description("Script ID (GUID). If both scriptId and path are provided, scriptId takes precedence.")] string? scriptId = null,
        [Description("File path of the script to open.")] string? path = null,
        CancellationToken cancellationToken = default)
    {
        if (scriptId != null)
        {
            if (!Guid.TryParse(scriptId, out var id))
                return "Invalid scriptId format. Expected a GUID.";

            var env = await api.OpenScriptByIdAsync(id, cancellationToken);
            return JsonSerializer.Serialize(env);
        }

        if (path != null)
        {
            var env = await api.OpenScriptByPathAsync(path, cancellationToken);
            return JsonSerializer.Serialize(env);
        }

        return "Either scriptId or path must be provided.";
    }
}
