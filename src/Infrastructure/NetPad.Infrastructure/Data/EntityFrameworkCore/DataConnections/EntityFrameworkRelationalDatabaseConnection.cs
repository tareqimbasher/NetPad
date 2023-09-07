using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace NetPad.Data.EntityFrameworkCore.DataConnections;

public abstract class EntityFrameworkRelationalDatabaseConnection : EntityFrameworkDatabaseConnection
{
    protected EntityFrameworkRelationalDatabaseConnection(Guid id, string name, DataConnectionType type, string entityFrameworkProviderName)
        : base(id, name, type, entityFrameworkProviderName)
    {
    }

    public override async Task<DataConnectionTestResult> TestConnectionAsync(IDataConnectionPasswordProtector passwordProtector)
    {
        await using var dbContext = CreateDbContext(passwordProtector);

        try
        {
            var relationalDbCreator = dbContext.GetService<IRelationalDatabaseCreator>();

            if (!await relationalDbCreator.ExistsAsync())
            {
                return new DataConnectionTestResult(false, $"Database \"{DatabaseName}\" does not exist");
            }

            // The previous check does not fail if DB exists but is not a database (ie. SQLite)
            _ = await relationalDbCreator.HasTablesAsync();

            return new DataConnectionTestResult(true);
        }
        catch (Exception ex)
        {
            return new DataConnectionTestResult(false, ex.Message);
        }
    }
}
