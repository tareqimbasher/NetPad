using System.Security.Cryptography;
using System.Text;
using NetPad.Application;
using NetPad.Data;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections;

internal class SQLiteDatabaseSchemaChangeDetectionStrategy(
    IDataConnectionResourcesRepository dataConnectionResourcesRepository,
    IDataConnectionPasswordProtector passwordProtector)
    : EntityFrameworkSchemaChangeDetectionStrategyBase(dataConnectionResourcesRepository, passwordProtector),
        IDataConnectionSchemaChangeDetectionStrategy
{
    public bool CanSupport(DataConnection dataConnection)
    {
        return dataConnection is SQLiteDatabaseConnection;
    }

    public async Task<bool?> DidSchemaChangeAsync(DataConnection dataConnection)
    {
        if (dataConnection is not SQLiteDatabaseConnection connection) return null;

        var schemaCompareInfo = await _dataConnectionResourcesRepository.GetSchemaCompareInfoAsync<SQLiteSchemaCompareInfo>(connection.Id);

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
        if (dataConnection is not SQLiteDatabaseConnection connection) return null;

        var hash = await GetSchemaHashAsync(connection);

        return hash == null ? null : new SQLiteSchemaCompareInfo(hash)
        {
            GeneratedOnAppVersion = AppIdentifier.PRODUCT_VERSION
        };
    }

    private async Task<string?> GetSchemaHashAsync(SQLiteDatabaseConnection connection)
    {
        var sb = new StringBuilder();

        await ExecuteSqlCommandAsync(connection, "SELECT sql FROM sqlite_schema", async result =>
        {
            while (await result.ReadAsync())
            {
                var sql = result["sql"] as string;
                if (sql != null) sb.Append(sql);
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

    private class SQLiteSchemaCompareInfo(string schemaHash) : SchemaCompareInfo(DateTime.UtcNow)
    {
        public string? SchemaHash { get; } = schemaHash;
    }
}
