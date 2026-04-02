using System.ComponentModel;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class GetScriptCodeTool
{
    [McpServerTool(Name = "get_script_code", ReadOnly = true), Description(
        "Get the code content of a script by its ID.")]
    public static async Task<string> GetScriptCode(
        NetPadApiClient api,
        [Description("Script ID (GUID)")] string scriptId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(scriptId, out var id))
            return "Invalid scriptId format. Expected a GUID.";

        return await api.GetScriptCodeAsync(id, cancellationToken);
    }
}
