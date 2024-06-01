using System.Security.Cryptography;
using System.Text;
using NetPad.Application;
using NetPad.Data;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections;

internal class PostgreSqlDatabaseSchemaChangeDetectionStrategy(
    IDataConnectionResourcesRepository dataConnectionResourcesRepository,
    IDataConnectionPasswordProtector passwordProtector)
    : EntityFrameworkSchemaChangeDetectionStrategyBase(dataConnectionResourcesRepository, passwordProtector),
        IDataConnectionSchemaChangeDetectionStrategy
{
    public bool CanSupport(DataConnection dataConnection)
    {
        return dataConnection is PostgreSqlDatabaseConnection;
    }

    public async Task<bool?> DidSchemaChangeAsync(DataConnection dataConnection)
    {
        if (dataConnection is not PostgreSqlDatabaseConnection connection) return null;

        var schemaCompareInfo = await _dataConnectionResourcesRepository.GetSchemaCompareInfoAsync<PostGreSqlSchemaCompareInfo>(connection.Id);

        if (schemaCompareInfo == null)
        {
            return null;
        }

        if (schemaCompareInfo.GeneratedUsingStaleAppVersion())
        {
            return true;
        }

        var hash = await GetSchemaHashAsync(connection);

        if (hash == null)
        {
            return null;
        }

        return hash != schemaCompareInfo.SchemaHash;
    }

    public async Task<SchemaCompareInfo?> GenerateSchemaCompareInfoAsync(DataConnection dataConnection)
    {
        if (dataConnection is not PostgreSqlDatabaseConnection connection) return null;

        var hash = await GetSchemaHashAsync(connection);

        return hash == null ? null : new PostGreSqlSchemaCompareInfo(hash)
        {
            GeneratedOnAppVersion = AppIdentifier.PRODUCT_VERSION
        };
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

    private class PostGreSqlSchemaCompareInfo(string schemaHash) : SchemaCompareInfo(DateTime.UtcNow)
    {
        public string? SchemaHash { get; } = schemaHash;
    }
}
