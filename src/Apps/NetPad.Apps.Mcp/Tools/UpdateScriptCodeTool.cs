using System.ComponentModel;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class UpdateScriptCodeTool
{
    [McpServerTool(Name = "update_script_code"), Description(
        "Update the code content of an open script in NetPad.")]
    public static async Task<string> UpdateScriptCode(
        NetPadApiClient api,
        [Description("Script ID (GUID)")] string scriptId,
        [Description("The new code content for the script")] string code,
        CancellationToken cancellationToken)
    {
        await api.UpdateScriptCodeAsync(Guid.Parse(scriptId), code, cancellationToken);
        return "Script code updated successfully.";
    }
}
