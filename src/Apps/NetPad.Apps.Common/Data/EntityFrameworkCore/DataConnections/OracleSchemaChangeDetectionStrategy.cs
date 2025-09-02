using System.Security.Cryptography;
using System.Text;
using NetPad.Application;
using NetPad.Data;
using NetPad.Data.Metadata;
using NetPad.Data.Metadata.ChangeDetection;
using NetPad.Data.Security;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections;

public class OracleSchemaChangeDetectionStrategy(IDataConnectionResourcesRepository dataConnectionResourcesRepository, IDataConnectionPasswordProtector passwordProtector)
    : EntityFrameworkSchemaChangeDetectionStrategyBase(dataConnectionResourcesRepository, passwordProtector), IDataConnectionSchemaChangeDetectionStrategy
{
    public bool CanSupport(DataConnection dataConnection)
    {
        return dataConnection is OracleDatabaseConnection;
    }
    public async Task<bool?> DidSchemaChangeAsync(DataConnection dataConnection)
    {
        if (!CanSupport(dataConnection))
        {
            return null;
        }
        var connection = (OracleDatabaseConnection)dataConnection;
        var schemaCompareInfo = await DataConnectionResourcesRepository.GetSchemaCompareInfoAsync<OracleSchemaCompareInfo>(connection.Id);

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
        if (!CanSupport(dataConnection))
        {
            return null;
        }

        var connection = (OracleDatabaseConnection)dataConnection;
        var hash = await GetSchemaHashAsync(connection);

        return hash == null ? null : new OracleSchemaCompareInfo(hash)
        {
            GeneratedOnAppVersion = AppIdentifier.PRODUCT_VERSION
        };
    }

    private async Task<string?> GetSchemaHashAsync(OracleDatabaseConnection connection)
    {
        string[] interestingColumns = ["owner", "table_name", "column_name", "nullable", "data_type"];
        string[] ignoredSchemas =
        [
            "'SYS'",
            "'SYSTEM'",
            "'OUTLN'",
            "'DBSFWUSER'",
            "'AUDSYS'",
            "'DBSNMP'",
            "'APPQOSSYS'",
            "'GSMADMIN_INTERNAL'",
            "'XDB'",
            "'WMSYS'",
            "'OJVMSYS'",
            "'CTXSYS'",
            "'ORDSYS'",
            "'ORDDATA'",
            "'OLAPSYS'",
            "'MDSYS'",
            "'LBACSYS'",
            "'DVSYS'"
        ];

        var sql = $"""
                   SELECT {string.Join(",", interestingColumns)}
                   FROM all_tab_columns
                   WHERE owner NOT IN ({string.Join(",", ignoredSchemas)})
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

        return sb.Length == 0 ? null : Convert.ToBase64String(CalculateHash(sb.ToString()));
    }

    private class OracleSchemaCompareInfo(string schemaHash) : SchemaCompareInfo(DateTime.UtcNow)
    {
        public string? SchemaHash { get; } = schemaHash;
    }
}
