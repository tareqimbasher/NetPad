using Microsoft.EntityFrameworkCore;
using NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;
using NetPad.Data;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections;

public abstract class EntityFrameworkDatabaseConnection(
    Guid id,
    string name,
    DataConnectionType type,
    string entityFrameworkProviderName,
    ScaffoldOptions? scaffoldOptions)
    : DatabaseConnection(id, name, type)
{
    public string EntityFrameworkProviderName { get; } = entityFrameworkProviderName;
    public ScaffoldOptions? ScaffoldOptions { get; } = scaffoldOptions;

    public abstract Task ConfigureDbContextOptionsAsync(DbContextOptionsBuilder builder, IDataConnectionPasswordProtector passwordProtector);

    public abstract Task<IEnumerable<string>> GetDatabasesAsync(IDataConnectionPasswordProtector passwordProtector);

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

    public DatabaseContext CreateDbContext(IDataConnectionPasswordProtector passwordProtector)
    {
        return DatabaseContext.Create(options => ConfigureDbContextOptionsAsync(options, passwordProtector));
    }
}
