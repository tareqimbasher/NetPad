using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class ListDataConnectionsTool
{
    [McpServerTool(Name = "list_data_connections", ReadOnly = true), Description(
        "List all configured database connections in NetPad.")]
    public static async Task<string> ListDataConnections(NetPadApiClient api, CancellationToken cancellationToken)
    {
        var response = await api.GetAllConnectionsAsync(cancellationToken);
        return JsonSerializer.Serialize(response);
    }
}
