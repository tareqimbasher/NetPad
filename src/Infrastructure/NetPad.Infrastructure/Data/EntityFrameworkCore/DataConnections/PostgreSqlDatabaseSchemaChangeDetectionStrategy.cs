using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NetPad.Data.EntityFrameworkCore.DataConnections;

public class PostgreSqlDatabaseSchemaChangeDetectionStrategy : EntityFrameworkSchemaChangeDetectionStrategyBase, IDataConnectionSchemaChangeDetectionStrategy
{
    public PostgreSqlDatabaseSchemaChangeDetectionStrategy(
        IDataConnectionResourcesRepository dataConnectionResourcesRepository,
        IDataConnectionPasswordProtector passwordProtector) : base(dataConnectionResourcesRepository, passwordProtector)
    {
    }

    public bool CanSupport(DataConnection dataConnection)
    {
        return dataConnection is PostgreSqlDatabaseConnection;
    }

    public async Task<bool?> DidSchemaChangeAsync(DataConnection dataConnection)
    {
        if (dataConnection is not PostgreSqlDatabaseConnection connection) return null;

        var hash = await GetSchemaHashAsync(connection);

        if (hash == null)
        {
            return null;
        }

        var schemaCompareInfo = await _dataConnectionResourcesRepository.GetSchemaCompareInfoAsync<PostGreSqlSchemaCompareInfo>(connection.Id);

        return schemaCompareInfo == null ? null : hash != schemaCompareInfo.SchemaHash;
    }

    public async Task<SchemaCompareInfo?> GenerateSchemaCompareInfoAsync(DataConnection dataConnection)
    {
        if (dataConnection is not PostgreSqlDatabaseConnection connection) return null;

        var hash = await GetSchemaHashAsync(connection);

        return hash == null ? null : new PostGreSqlSchemaCompareInfo(hash);
    }

    private async Task<string?> GetSchemaHashAsync(PostgreSqlDatabaseConnection connection)
    {
        var interestingColumns = new[] { "table_schema", "table_name", "column_name", "is_nullable", "data_type", "is_identity" };

        var sql = $"""
                   select {interestingColumns.JoinToString(",")}
                   from information_schema.columns
                   where table_schema not in ('pg_catalog', 'information_schema')
                     and table_schema not like 'pg_toast%'
                   order by table_schema, table_name, column_name
                   """;

        var sb = new StringBuilder();

        await ExecuteSqlCommandAsync(connection, sql, async result =>
        {
            while (await result.ReadAsync())
            {
                foreach (var column in interestingColumns)
                {
                    var value = result[column] as string;
                    sb.Append(value);
                }
            }
        });

        if (sb.Length == 0)
        {
            return null;
        }

        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));

        return Convert.ToHexString(hash);
    }

    private class PostGreSqlSchemaCompareInfo : SchemaCompareInfo
    {
        public PostGreSqlSchemaCompareInfo(string schemaHash) : base(DateTime.UtcNow)
        {
            SchemaHash = schemaHash;
        }

        public string? SchemaHash { get; }
    }
}
