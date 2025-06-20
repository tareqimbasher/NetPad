using System.Security.Cryptography;
using System.Text;
using NetPad.Application;
using NetPad.Data;
using NetPad.Data.Metadata;
using NetPad.Data.Metadata.ChangeDetection;
using NetPad.Data.Security;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections;

internal class MariaDbDatabaseSchemaChangeDetectionStrategy(
    IDataConnectionResourcesRepository dataConnectionResourcesRepository,
    IDataConnectionPasswordProtector passwordProtector)
    : EntityFrameworkSchemaChangeDetectionStrategyBase(dataConnectionResourcesRepository, passwordProtector),
        IDataConnectionSchemaChangeDetectionStrategy
{
    public bool CanSupport(DataConnection dataConnection)
    {
        return dataConnection is MariaDbDatabaseConnection;
    }

    public async Task<bool?> DidSchemaChangeAsync(DataConnection dataConnection)
    {
        if (dataConnection is not MariaDbDatabaseConnection connection)
        {
            return null;
        }

        var schemaCompareInfo = await _dataConnectionResourcesRepository.GetSchemaCompareInfoAsync<MariaDbSchemaCompareInfo>(connection.Id);

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
        if (dataConnection is not MariaDbDatabaseConnection connection)
        {
            return null;
        }

        var hash = await GetSchemaHashAsync(connection);

        return hash == null ? null : new MariaDbSchemaCompareInfo(hash)
        {
            GeneratedOnAppVersion = AppIdentifier.PRODUCT_VERSION
        };
    }

    private async Task<string?> GetSchemaHashAsync(MariaDbDatabaseConnection connection)
    {
        string[] interestingColumns = [ "table_schema", "table_name", "column_name", "is_nullable", "data_type" ];

        var sql = $"""
                    SELECT {string.Join(",", interestingColumns)}
                    FROM information_schema.columns
                    WHERE table_schema NOT IN ('mysql', 'performance_schema', 'information_schema', 'sys')
                    ORDER BY table_schema, table_name, column_name;
                    """;

        StringBuilder sb = new();

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

        return Convert.ToBase64String(hash);
    }

    private class MariaDbSchemaCompareInfo(string schemaHash) : SchemaCompareInfo(DateTime.UtcNow)
    {
        public string? SchemaHash { get; } = schemaHash;
    }
}
