using Microsoft.EntityFrameworkCore;
using NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;
using NetPad.Data;
using NetPad.Data.Security;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections.PostgreSql;

public sealed class PostgreSqlDatabaseConnection(Guid id, string name, ScaffoldOptions? scaffoldOptions = null)
    : EntityFrameworkDatabaseConnection(id, name, DataConnectionType.PostgreSQL, ProviderName, scaffoldOptions),
        IPostgreSqlConnection
{
    public const string ProviderName = "Npgsql.EntityFrameworkCore.PostgreSQL";

    public override string GetConnectionString(IDataConnectionPasswordProtector passwordProtector)
        => PostgreSqlUtils.GetConnectionString(this, DatabaseName, passwordProtector);

    public override void ConfigureDbContextOptions(
        DbContextOptionsBuilder builder,
        IDataConnectionPasswordProtector passwordProtector)
    {
        builder.UseNpgsql(GetConnectionString(passwordProtector));
    }

    public override Task<IReadOnlyList<string>> GetDatabasesAsync(IDataConnectionPasswordProtector passwordProtector)
        => PostgreSqlUtils.GetDatabasesAsync(this, passwordProtector);
}
