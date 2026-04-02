using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class GetDatabaseStructureTool
{
    [McpServerTool(Name = "get_database_structure", ReadOnly = true), Description(
        "Get the schema and table structure of a database connection, " +
        "including tables, columns, and their types. " +
        "Use list_data_connections to find available connection IDs.")]
    public static async Task<string> GetDatabaseStructure(
        NetPadApiClient api,
        [Description("Data connection ID (GUID)")] string connectionId,
        CancellationToken cancellationToken)
    {
        var structure = await api.GetDatabaseStructureAsync(Guid.Parse(connectionId), cancellationToken);
        return JsonSerializer.Serialize(structure, JsonDefaults.IndentedOptions);
    }
}
