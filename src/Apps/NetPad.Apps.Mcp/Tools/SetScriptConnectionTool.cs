using System.ComponentModel;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class SetScriptConnectionTool
{
    [McpServerTool(Name = "set_script_connection"), Description(
        "Set or remove the data connection for a script. " +
        "Setting a connection allows the script to access that database. " +
        "Use list_data_connections to find available connection IDs.")]
    public static async Task<string> SetScriptConnection(
        NetPadApiClient api,
        [Description("Script ID (GUID)")] string scriptId,
        [Description("Data connection ID (GUID) to set, or omit to remove the current connection")] string? dataConnectionId = null,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(scriptId, out var id))
            return "Invalid scriptId format. Expected a GUID.";

        Guid? connId = null;
        if (dataConnectionId != null)
        {
            if (!Guid.TryParse(dataConnectionId, out var parsed))
                return "Invalid dataConnectionId format. Expected a GUID.";
            connId = parsed;
        }

        await api.SetScriptDataConnectionAsync(id, connId, cancellationToken);

        return connId.HasValue
            ? "Data connection set successfully."
            : "Data connection removed from script.";
    }
}
