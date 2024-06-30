using NetPad.Application;
using NetPad.Data;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections;

internal class MsSqlServerDatabaseSchemaChangeDetectionStrategy(
    IDataConnectionResourcesRepository dataConnectionResourcesRepository,
    IDataConnectionPasswordProtector passwordProtector)
    : EntityFrameworkSchemaChangeDetectionStrategyBase(dataConnectionResourcesRepository, passwordProtector),
        IDataConnectionSchemaChangeDetectionStrategy
{
    public bool CanSupport(DataConnection dataConnection)
    {
        return dataConnection is MsSqlServerDatabaseConnection;
    }

    public async Task<bool?> DidSchemaChangeAsync(DataConnection dataConnection)
    {
        if (dataConnection is not MsSqlServerDatabaseConnection connection) return null;

        var schemaCompareInfo = await _dataConnectionResourcesRepository.GetSchemaCompareInfoAsync<MsSqlServerSchemaCompareInfo>(connection.Id);

        if (schemaCompareInfo == null)
        {
            return null;
        }

        if (schemaCompareInfo.GeneratedUsingStaleAppVersion())
        {
            return true;
        }

        var lastSchemaModificationTime = await GetLastSchemaModificationTimeFromServerAsync(connection);

        if (lastSchemaModificationTime == null)
        {
            return null;
        }

        return lastSchemaModificationTime > schemaCompareInfo.LastSchemaModificationTime;
    }

    public async Task<SchemaCompareInfo?> GenerateSchemaCompareInfoAsync(DataConnection dataConnection)
    {
        if (dataConnection is not MsSqlServerDatabaseConnection connection) return null;

        var lastSchemaModificationTime = await GetLastSchemaModificationTimeFromServerAsync(connection);

        return lastSchemaModificationTime == null ? null : new MsSqlServerSchemaCompareInfo(lastSchemaModificationTime.Value)
        {
            GeneratedOnAppVersion = AppIdentifier.PRODUCT_VERSION
        };
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

    private class MsSqlServerSchemaCompareInfo(DateTime lastSchemaModificationTime) : SchemaCompareInfo(DateTime.UtcNow)
    {
        public DateTime LastSchemaModificationTime { get; } = lastSchemaModificationTime;
    }
}
