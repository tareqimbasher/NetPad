using Microsoft.EntityFrameworkCore;
using NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;
using NetPad.Data;
using NetPad.Data.Security;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections;

/// <summary>
/// A data connection that uses EntityFramework to connect to a database.
/// </summary>
public abstract class EntityFrameworkDatabaseConnection(
    Guid id,
    string name,
    DataConnectionType type,
    string entityFrameworkProviderName,
    ScaffoldOptions? scaffoldOptions)
    : DatabaseConnection(id, name, type), IEntityFrameworkDatabaseConnection
{
    public string EntityFrameworkProviderName { get; } = entityFrameworkProviderName;

    public ScaffoldOptions? ScaffoldOptions =>
        (Server as EntityFrameworkDatabaseServerConnection)?.ScaffoldOptions ?? scaffoldOptions;

    public abstract void ConfigureDbContextOptions(
        DbContextOptionsBuilder builder,
        IDataConnectionPasswordProtector passwordProtector);

    public override async Task<DataConnectionTestResult> TestConnectionAsync(
        IDataConnectionPasswordProtector passwordProtector)
    {
        await using var dbContext = DatabaseContext.Create(this, passwordProtector);
        return await dbContext.TestConnectionAsync();
    }
}
