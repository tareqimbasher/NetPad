using System.Data.Common;
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
        IEntityFrameworkDatabaseConnection connection,
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
            var serverVersion = GetServerVersion(connection);
            await connection.CloseAsync();
            return DataConnectionTestResult.Succeeded(serverVersion);
        }
        catch (Exception ex)
        {
            return DataConnectionTestResult.Failed(ex.Message);
        }
    }

    private static string? GetServerVersion(DbConnection connection)
    {
        // Some ADO.NET providers throw or return an empty string.
        try
        {
            var version = connection.ServerVersion;
            return string.IsNullOrWhiteSpace(version) ? null : version;
        }
        catch
        {
            return null;
        }
    }
}

public static class DatabaseContextExtensions
{
    public static DatabaseContext CreateDbContext(
        this IEntityFrameworkDatabaseConnection connection,
        IDataConnectionPasswordProtector passwordProtector)
    {
        return DatabaseContext.Create(connection, passwordProtector);
    }
}
