using Microsoft.EntityFrameworkCore;
using NetPad.Data;
using NetPad.Data.Security;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections.MsSqlServer;

public sealed class MsSqlServerDatabaseServerConnection(Guid id, string name)
    : EntityFrameworkDatabaseServerConnection(id, name, DataConnectionType.MSSQLServer), IMsSqlServerConnection
{
    public override DatabaseConnection CreateDatabaseConnection(string databaseName)
    {
        var connection = new MsSqlServerDatabaseConnection(Guid.NewGuid(), databaseName);
        connection.DatabaseName = databaseName;
        connection.SetServer(this);
        return connection;
    }

    public override string GetConnectionString(IDataConnectionPasswordProtector passwordProtector)
        => MsSqlServerUtils.GetConnectionString(this, null, passwordProtector);

    public override void ConfigureDbContextOptions(
        DbContextOptionsBuilder builder,
        IDataConnectionPasswordProtector passwordProtector)
    {
        builder.UseSqlServer(GetConnectionString(passwordProtector));
    }

    public override Task<IReadOnlyList<string>> GetDatabasesAsync(IDataConnectionPasswordProtector passwordProtector)
        => MsSqlServerUtils.GetDatabasesAsync(this, passwordProtector);
}
