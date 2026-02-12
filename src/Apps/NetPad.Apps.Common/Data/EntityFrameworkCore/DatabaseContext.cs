using Microsoft.EntityFrameworkCore;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections;
using NetPad.Data;
using NetPad.Data.Security;

namespace NetPad.Apps.Data.EntityFrameworkCore;

/// <summary>
/// A generic database context used by the host to test data connections, get listing of databases...etc.
/// </summary>
public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    public static DatabaseContext Create(Action<DbContextOptionsBuilder<DatabaseContext>> configure)
    {
        var dbOptionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
        configure(dbOptionsBuilder);
        return new DatabaseContext(dbOptionsBuilder.Options);
    }

    public static DatabaseContext Create(
        EntityFrameworkDatabaseConnection connection,
        IDataConnectionPasswordProtector passwordProtector)
    {
        return Create(options => connection.ConfigureDbContextOptions(options, passwordProtector));
    }

    public static DatabaseContext Create(
        EntityFrameworkDatabaseServerConnection connection,
        IDataConnectionPasswordProtector passwordProtector)
    {
        return Create(options => connection.ConfigureDbContextOptions(options, passwordProtector));
    }

    public async Task<DataConnectionTestResult> TestConnectionAsync()
    {
        try
        {
            var connection = Database.GetDbConnection();
            await connection.OpenAsync();
            await connection.CloseAsync();
            return new DataConnectionTestResult(true);
        }
        catch (Exception ex)
        {
            return new DataConnectionTestResult(false, ex.Message);
        }
    }
}
