using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class DuplicateScriptTool
{
    [McpServerTool(Name = "duplicate_script", Destructive = false), Description(
        "Duplicate an open script in NetPad. Creates a copy with a new name and opens it in the editor.")]
    public static async Task<string> DuplicateScript(
        NetPadApiClient api,
        [Description("Script ID (GUID) of the script to duplicate")] string scriptId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(scriptId, out var id))
            return "Invalid scriptId format. Expected a GUID.";

        var script = await api.DuplicateScriptAsync(id, cancellationToken);
        return JsonSerializer.Serialize(script);
    }
}
