using Microsoft.EntityFrameworkCore;
using NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;
using NetPad.Data;
using NetPad.Data.Security;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections.MySql;

public sealed class MySqlDatabaseServerConnection(Guid id, string name, ScaffoldOptions? scaffoldOptions = null)
    : EntityFrameworkDatabaseServerConnection(id, name, DataConnectionType.MySQL, scaffoldOptions), IMySqlConnection
{
    public override DatabaseConnection CreateDatabaseConnection(string databaseName)
    {
        var connection = new MySqlDatabaseConnection(Guid.NewGuid(), databaseName);
        connection.DatabaseName = databaseName;
        connection.SetServer(this);
        return connection;
    }

    public override string GetConnectionString(IDataConnectionPasswordProtector passwordProtector)
        => MySqlUtils.GetConnectionString(this, null, passwordProtector);

    public override void ConfigureDbContextOptions(
        DbContextOptionsBuilder builder,
        IDataConnectionPasswordProtector passwordProtector)
    {
        var connectionString = GetConnectionString(passwordProtector);
        var serverVersion = MySqlServerVersion.AutoDetect(connectionString);
        builder.UseMySql(connectionString, serverVersion);
    }

    public override Task<IReadOnlyList<string>> GetDatabasesAsync(IDataConnectionPasswordProtector passwordProtector)
        => MySqlUtils.GetDatabasesAsync(this, passwordProtector);
}
