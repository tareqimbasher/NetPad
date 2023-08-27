using System;
using System.Threading.Tasks;

namespace NetPad.Data.EntityFrameworkCore.DataConnections;

public class MsSqlServerDatabaseSchemaChangeDetectionStrategy : EntityFrameworkSchemaChangeDetectionStrategyBase, IDataConnectionSchemaChangeDetectionStrategy
{
    public MsSqlServerDatabaseSchemaChangeDetectionStrategy(
        IDataConnectionResourcesRepository dataConnectionResourcesRepository,
        IDataConnectionPasswordProtector passwordProtector) : base(dataConnectionResourcesRepository, passwordProtector)
    {
    }

    public bool CanSupport(DataConnection dataConnection)
    {
        return dataConnection is MsSqlServerDatabaseConnection;
    }

    public async Task<bool?> DidSchemaChangeAsync(DataConnection dataConnection)
    {
        if (dataConnection is not MsSqlServerDatabaseConnection connection) return null;

        var lastSchemaModificationTime = await GetLastSchemaModificationTimeFromServerAsync(connection);

        if (lastSchemaModificationTime == null)
        {
            return null;
        }

        var schemaCompareInfo = await _dataConnectionResourcesRepository.GetSchemaCompareInfoAsync<MsSqlServerSchemaCompareInfo>(connection.Id);

        return schemaCompareInfo == null ? null : lastSchemaModificationTime > schemaCompareInfo.LastSchemaModificationTime;
    }

    public async Task<SchemaCompareInfo?> GenerateSchemaCompareInfoAsync(DataConnection dataConnection)
    {
        if (dataConnection is not MsSqlServerDatabaseConnection connection) return null;

        var lastSchemaModificationTime = await GetLastSchemaModificationTimeFromServerAsync(connection);

        return lastSchemaModificationTime == null ? null : new MsSqlServerSchemaCompareInfo(lastSchemaModificationTime.Value);
    }

    private async Task<DateTime?> GetLastSchemaModificationTimeFromServerAsync(MsSqlServerDatabaseConnection connection)
    {
        DateTime? lastSchemaModificationTime = null;

        await ExecuteSqlCommandAsync(connection,
            "SELECT max(modify_date) AS LastModifiedDate FROM sys.objects WHERE is_ms_shipped = 'False' AND type IN ('F', 'PK', 'U', 'V');",
            async result =>
            {
                if (await result.ReadAsync())
                {
                    lastSchemaModificationTime = result["LastModifiedDate"] as DateTime?;
                }
            });

        return lastSchemaModificationTime;
    }

    private class MsSqlServerSchemaCompareInfo : SchemaCompareInfo
    {
        public MsSqlServerSchemaCompareInfo(DateTime lastSchemaModificationTime) : base(DateTime.UtcNow)
        {
            LastSchemaModificationTime = lastSchemaModificationTime;
        }

        public DateTime LastSchemaModificationTime { get; }
    }
}
