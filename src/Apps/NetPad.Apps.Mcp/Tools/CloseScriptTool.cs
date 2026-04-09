using System.ComponentModel;
using System.Net;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class CloseScriptTool
{
    [McpServerTool(Name = "close_script", Destructive = true, Idempotent = true), Description(
         "Close a script in the NetPad editor. This removes the script's tab and discards any unsaved changes. " +
         "If you want to preserve changes, call save_script before closing. " +
         "If the script is not open, this has no effect.")]
    public static async Task<string> CloseScript(
        NetPadApiClient api,
        [Description("Script ID (GUID)")] string scriptId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(scriptId, out var id))
            return "Invalid scriptId format. Expected a GUID.";

        try
        {
            await api.CloseScriptAsync(id, cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return "Script is not open.";
        }

        return "Script closed.";
    }
}
