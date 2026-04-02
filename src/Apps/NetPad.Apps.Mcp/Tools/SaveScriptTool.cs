using System.ComponentModel;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class SaveScriptTool
{
    [McpServerTool(Name = "save_script"), Description(
        "Save a script to disk. If the script is new and has never been saved, " +
        "a save dialog may appear in the NetPad GUI.")]
    public static async Task<string> SaveScript(
        NetPadApiClient api,
        [Description("Script ID (GUID)")] string scriptId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(scriptId, out var id))
            return "Invalid scriptId format. Expected a GUID.";

        var saved = await api.SaveScriptAsync(id, cancellationToken);

        return saved
            ? "Script saved successfully."
            : "Script was not saved (user may have cancelled the save dialog).";
    }
}
