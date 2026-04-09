using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class TestDataConnectionTool
{
    [McpServerTool(Name = "test_data_connection", ReadOnly = true, Destructive = false, Idempotent = true), Description(
        "Test if a data connection is valid and can be reached. Use list_data_connections to find connection IDs if needed.")]
    public static async Task<string> TestDataConnection(
        NetPadApiClient api,
        [Description("Data connection ID (GUID)")] string connectionId,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(connectionId, out var id))
            return "Invalid connectionId format. Expected a GUID.";

        var result = await api.TestDataConnectionAsync(id, cancellationToken);
        return JsonSerializer.Serialize(result);
    }
}
