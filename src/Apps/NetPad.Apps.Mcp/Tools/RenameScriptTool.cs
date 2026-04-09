using System.ComponentModel;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class RenameScriptTool
{
    [McpServerTool(Name = "rename_script", Destructive = false, Idempotent = true), Description(
        "Rename an open script in NetPad.")]
    public static async Task<string> RenameScript(
        NetPadApiClient api,
        [Description("Script ID (GUID)")] string scriptId,
        [Description("The new name for the script")] string newName,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(scriptId, out var id))
            return "Invalid scriptId format. Expected a GUID.";

        await api.RenameScriptAsync(id, newName, cancellationToken);
        return $"Script renamed to '{newName}'.";
    }
}
