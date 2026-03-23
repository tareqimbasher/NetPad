using Microsoft.EntityFrameworkCore;
using NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;
using NetPad.Data;
using NetPad.Data.Security;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections.PostgreSql;

public sealed class PostgreSqlDatabaseServerConnection(Guid id, string name, ScaffoldOptions? scaffoldOptions = null)
    : EntityFrameworkDatabaseServerConnection(id, name, DataConnectionType.PostgreSQL, scaffoldOptions), IPostgreSqlConnection
{
    public override DatabaseConnection CreateDatabaseConnection(string databaseName)
    {
        var connection = new PostgreSqlDatabaseConnection(Guid.NewGuid(), databaseName);
        connection.DatabaseName = databaseName;
        connection.SetServer(this);
        return connection;
    }

    public override string GetConnectionString(IDataConnectionPasswordProtector passwordProtector)
        => PostgreSqlUtils.GetConnectionString(this, null, passwordProtector);


    public override void ConfigureDbContextOptions(
        DbContextOptionsBuilder builder,
        IDataConnectionPasswordProtector passwordProtector)
    {
        builder.UseNpgsql(GetConnectionString(passwordProtector));
    }

    public override Task<IReadOnlyList<string>> GetDatabasesAsync(IDataConnectionPasswordProtector passwordProtector)
        => PostgreSqlUtils.GetDatabasesAsync(this, passwordProtector);
}
