using Microsoft.EntityFrameworkCore;
using NetPad.Data;
using NetPad.Data.Security;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections;

public abstract class EntityFrameworkDatabaseServerConnection(
    Guid id,
    string name,
    DataConnectionType type)
    : DatabaseServerConnection(id, name, type)
{
    public abstract Task ConfigureDbContextOptionsAsync(DbContextOptionsBuilder builder, IDataConnectionPasswordProtector passwordProtector);

    public override async Task<DataConnectionTestResult> TestConnectionAsync(IDataConnectionPasswordProtector passwordProtector)
    {
        await using var dbContext = CreateDbContext(passwordProtector);

        try
        {
            var connection = dbContext.Database.GetDbConnection();
            await connection.OpenAsync();
            await connection.CloseAsync();
            return new DataConnectionTestResult(true);
        }
        catch (Exception ex)
        {
            return new DataConnectionTestResult(false, ex.Message);
        }
    }

    protected DatabaseContext CreateDbContext(IDataConnectionPasswordProtector passwordProtector)
    {
        return DatabaseContext.Create(options => ConfigureDbContextOptionsAsync(options, passwordProtector));
    }
}
