using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using NetPad.Apps.Mcp.Dtos;

namespace NetPad.Apps.Mcp.Tools;

[McpServerToolType]
public class GetDatabaseStructureTool
{
    [McpServerTool(Name = "get_database_structure", ReadOnly = true, Destructive = false, Idempotent = true),
     Description(
         "Get the structure of a database. " +
         "Call without filters to get an overview of schemas and table names, " +
         "then use schemaName and/or tableName to get full column details for specific tables. " +
         "Use list_data_connections to find connection IDs.")]
    public static async Task<string> GetDatabaseStructure(
        NetPadApiClient api,
        [Description("Data connection ID (GUID)")]
        string connectionId,
        [Description("Filter to a specific schema (case-insensitive)")]
        string? schemaName = null,
        [Description("Filter to tables whose name contains this value (case-insensitive)")]
        string? tableName = null,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(connectionId, out var id))
            return "Invalid connectionId format. Expected a GUID.";

        var structure = await api.GetDatabaseStructureAsync(id, cancellationToken);

        bool hasFilter = schemaName != null || tableName != null;

        if (hasFilter)
        {
            var filtered = FilterStructure(structure, schemaName, tableName);
            return JsonSerializer.Serialize(filtered, JsonDefaults.IgnoreNullOptions);
        }

        return JsonSerializer.Serialize(Summarize(structure), JsonDefaults.IgnoreNullOptions);
    }

    private static object FilterStructure(DatabaseStructureDto structure, string? schemaName, string? tableName)
    {
        var schemas = structure.Schemas.AsEnumerable();

        if (schemaName != null)
        {
            schemas = schemas.Where(s =>
                string.Equals(s.Name, schemaName, StringComparison.OrdinalIgnoreCase));
        }

        if (tableName != null)
        {
            schemas = schemas.Select(s => new DatabaseSchemaDto
            {
                Name = s.Name,
                Tables = s.Tables
                    .Where(t => t.Name.Contains(tableName, StringComparison.OrdinalIgnoreCase))
                    .ToArray()
            }).Where(s => s.Tables.Length > 0);
        }

        return new
        {
            databaseName = structure.DatabaseName,
            schemas = schemas.Select(s => new
            {
                name = s.Name,
                tables = s.Tables.Select(t => new
                {
                    name = t.Name,
                    dbSet = t.DisplayName,
                    columns = t.Columns.Select(c => new
                    {
                        name = c.Name,
                        dbType = c.Type,
                        clr = c.ClrType,
                        pk = c.IsPrimaryKey,
                        fk = c.IsForeignKey
                    }).ToArray()
                }).ToArray()
            }).ToArray()
        };
    }

    private static object Summarize(DatabaseStructureDto structure)
    {
        return new
        {
            databaseName = structure.DatabaseName,
            schemas = structure.Schemas.Select(s => new
            {
                name = s.Name,
                tables = s.Tables.Select(t => t.Name).ToArray()
            }).ToArray()
        };
    }
}
