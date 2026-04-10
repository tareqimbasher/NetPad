using System.ComponentModel;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class RefreshDataConnectionTool
{
    [McpServerTool(Name = "refresh_data_connection", ReadOnly = false, Destructive = false, Idempotent = true), Description(
        "Refresh cached database metadata for a data connection. Use this if get_database_structure returns " +
        "stale or outdated schema information. Use list_data_connections to find connection IDs if needed.")]
    public static async Task<string> RefreshDataConnection(
        NetPadApiClient api,
        [Description("Data connection ID (GUID)")] string connectionId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(connectionId, out var id))
            return "Invalid connectionId format. Expected a GUID.";

        await api.RefreshDataConnectionAsync(id, cancellationToken);
        return $"Data connection {connectionId} refresh initiated. The database metadata cache will be updated in the background.";
    }
}
