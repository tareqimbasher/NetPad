using System.ComponentModel;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class UpdateScriptCodeTool
{
    [McpServerTool(Name = "update_script_code", Destructive = false, Idempotent = true), Description(
        "Update the code content of an open script in NetPad.")]
    public static async Task<string> UpdateScriptCode(
        NetPadApiClient api,
        [Description("Script ID (GUID)")] string scriptId,
        [Description("The new code content for the script")] string code,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(scriptId, out var id))
            return "Invalid scriptId format. Expected a GUID.";

        await api.UpdateScriptCodeAsync(id, code, cancellationToken);
        return "Script code updated successfully.";
    }
}
