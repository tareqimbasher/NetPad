using Microsoft.EntityFrameworkCore;
using NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;
using NetPad.Data;
using NetPad.Data.Security;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections;

public abstract class EntityFrameworkDatabaseServerConnection(
    Guid id,
    string name,
    DataConnectionType type,
    ScaffoldOptions? scaffoldOptions = null)
    : DatabaseServerConnection(id, name, type), IEntityFrameworkDatabaseConnection
{
    public ScaffoldOptions? ScaffoldOptions { get; } = scaffoldOptions;
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
