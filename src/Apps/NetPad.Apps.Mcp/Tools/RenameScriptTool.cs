using System.ComponentModel;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class RenameScriptTool
{
    [McpServerTool(Name = "rename_script"), Description(
        "Rename an open script in NetPad.")]
    public static async Task<string> RenameScript(
        NetPadApiClient api,
        [Description("Script ID (GUID)")] string scriptId,
        [Description("The new name for the script")] string newName,
        CancellationToken cancellationToken)
    {
        await api.RenameScriptAsync(Guid.Parse(scriptId), newName, cancellationToken);
        return $"Script renamed to '{newName}'.";
    }
}
