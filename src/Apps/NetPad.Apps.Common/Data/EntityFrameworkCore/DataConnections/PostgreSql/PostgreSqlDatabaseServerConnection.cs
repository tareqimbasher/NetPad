using Microsoft.EntityFrameworkCore;
using NetPad.Data;
using NetPad.Data.Security;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections.PostgreSql;

public sealed class PostgreSqlDatabaseServerConnection(Guid id, string name)
    : EntityFrameworkDatabaseServerConnection(id, name, DataConnectionType.PostgreSQL), IPostgreSqlConnection
{
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
