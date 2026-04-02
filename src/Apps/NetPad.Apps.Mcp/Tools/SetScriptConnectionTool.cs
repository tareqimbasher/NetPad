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
        var connId = dataConnectionId != null ? Guid.Parse(dataConnectionId) : (Guid?)null;
        await api.SetScriptDataConnectionAsync(Guid.Parse(scriptId), connId, cancellationToken);

        return connId.HasValue
            ? "Data connection set successfully."
            : "Data connection removed from script.";
    }
}
