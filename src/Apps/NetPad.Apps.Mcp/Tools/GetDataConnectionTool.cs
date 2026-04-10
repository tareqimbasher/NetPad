using System.ComponentModel;
using System.Text.Json.Nodes;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class GetDataConnectionTool
{
    private static readonly string[] _sensitiveProperties = ["password", "userId"];

    [McpServerTool(Name = "get_data_connection", ReadOnly = true, Destructive = false, Idempotent = true), Description(
         "Get details of a single data connection or database server by ID, including its type, " +
         "host, port, database name, and other configuration. " +
         "Use list_data_connections to find connection and server IDs if needed.")]
    public static async Task<string> GetDataConnection(
        NetPadApiClient api,
        [Description("Data connection or database server ID (GUID)")]
        string connectionId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(connectionId, out var id))
            return "Invalid connectionId format. Expected a GUID.";

        var json = await api.GetDataConnectionAsync(id, cancellationToken)
                   ?? await api.GetDatabaseServerAsync(id, cancellationToken);

        if (json == null)
            return $"Data connection or server not found: {connectionId}";

        return StripSensitiveProperties(json);
    }

    private static string StripSensitiveProperties(string json)
    {
        var node = JsonNode.Parse(json);
        if (node is not JsonObject obj) return json;

        foreach (var prop in _sensitiveProperties)
        {
            obj.Remove(prop);
        }

        return obj.ToJsonString();
    }
}
