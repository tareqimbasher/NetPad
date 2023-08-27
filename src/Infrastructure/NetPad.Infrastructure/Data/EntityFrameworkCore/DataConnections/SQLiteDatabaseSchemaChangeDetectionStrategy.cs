using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace NetPad.Data.EntityFrameworkCore.DataConnections;

public class SQLiteDatabaseSchemaChangeDetectionStrategy : EntityFrameworkSchemaChangeDetectionStrategyBase, IDataConnectionSchemaChangeDetectionStrategy
{
    public SQLiteDatabaseSchemaChangeDetectionStrategy(
        IDataConnectionResourcesRepository dataConnectionResourcesRepository,
        IDataConnectionPasswordProtector passwordProtector) : base(dataConnectionResourcesRepository, passwordProtector)
    {
    }

    public bool CanSupport(DataConnection dataConnection)
    {
        return dataConnection is SQLiteDatabaseConnection;
    }

    public async Task<bool?> DidSchemaChangeAsync(DataConnection dataConnection)
    {
        if (dataConnection is not SQLiteDatabaseConnection connection) return null;

        var hash = await GetSchemaHashAsync(connection);

        if (hash == null)
        {
            return null;
        }

        var schemaCompareInfo = await _dataConnectionResourcesRepository.GetSchemaCompareInfoAsync<SQLiteSchemaCompareInfo>(connection.Id);

        return schemaCompareInfo == null ? null : hash != schemaCompareInfo.SchemaHash;
    }

    public async Task<SchemaCompareInfo?> GenerateSchemaCompareInfoAsync(DataConnection dataConnection)
    {
        if (dataConnection is not SQLiteDatabaseConnection connection) return null;

        var hash = await GetSchemaHashAsync(connection);

        return hash == null ? null : new SQLiteSchemaCompareInfo(hash);
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

    private class SQLiteSchemaCompareInfo : SchemaCompareInfo
    {
        public SQLiteSchemaCompareInfo(string schemaHash) : base(DateTime.UtcNow)
        {
            SchemaHash = schemaHash;
        }

        public string? SchemaHash { get; }
    }
}
