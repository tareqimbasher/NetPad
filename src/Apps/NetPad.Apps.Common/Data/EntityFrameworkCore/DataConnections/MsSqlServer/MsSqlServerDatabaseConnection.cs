using Microsoft.EntityFrameworkCore;
using NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;
using NetPad.Data;
using NetPad.Data.Security;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections.MsSqlServer;

public sealed class MsSqlServerDatabaseConnection(Guid id, string name, ScaffoldOptions? scaffoldOptions = null)
    : EntityFrameworkDatabaseConnection(id, name, DataConnectionType.MSSQLServer, ProviderName, scaffoldOptions),
        IMsSqlServerConnection
{
    public const string ProviderName = "Microsoft.EntityFrameworkCore.SqlServer";

    public override string GetConnectionString(IDataConnectionPasswordProtector passwordProtector)
        => MsSqlServerUtils.GetConnectionString(this, DatabaseName, passwordProtector);

    public override void ConfigureDbContextOptions(
        DbContextOptionsBuilder builder,
        IDataConnectionPasswordProtector passwordProtector)
    {
        builder.UseSqlServer(GetConnectionString(passwordProtector));
    }

    public override Task<IReadOnlyList<string>> GetDatabasesAsync(IDataConnectionPasswordProtector passwordProtector)
        => MsSqlServerUtils.GetDatabasesAsync(this, passwordProtector);
}
