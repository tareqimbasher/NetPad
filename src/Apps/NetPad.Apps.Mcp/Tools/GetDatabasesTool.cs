using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class GetDatabasesTool
{
    [McpServerTool(Name = "get_databases", ReadOnly = true, Destructive = false, Idempotent = true), Description(
        "Get the list of databases available on a data connection or database server. " +
        "Use list_data_connections to find connection and server IDs if needed.")]
    public static async Task<string> GetDatabases(
        NetPadApiClient api,
        [Description("Data connection or database server ID (GUID)")] string connectionId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(connectionId, out var id))
            return "Invalid connectionId format. Expected a GUID.";

        var databases = await api.GetDatabasesAsync(id, cancellationToken);
        return JsonSerializer.Serialize(databases);
    }
}
