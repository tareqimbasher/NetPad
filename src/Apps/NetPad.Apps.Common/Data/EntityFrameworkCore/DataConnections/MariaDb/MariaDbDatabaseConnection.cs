using Microsoft.EntityFrameworkCore;
using NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;
using NetPad.Data;
using NetPad.Data.Security;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections.MariaDb;

public sealed class MariaDbDatabaseConnection(Guid id, string name, ScaffoldOptions? scaffoldOptions = null)
    : EntityFrameworkDatabaseConnection(id, name, DataConnectionType.MariaDB, ProviderName, scaffoldOptions),
        IMariaDbConnection
{
    public const string ProviderName = "Pomelo.EntityFrameworkCore.MySql";

    public override string GetConnectionString(IDataConnectionPasswordProtector passwordProtector)
        => MariaDbUtils.GetConnectionString(this, DatabaseName, passwordProtector);

    public override void ConfigureDbContextOptions(
        DbContextOptionsBuilder builder,
        IDataConnectionPasswordProtector passwordProtector)
    {
        var connectionString = GetConnectionString(passwordProtector);

        var serverVersion = MariaDbServerVersion.AutoDetect(connectionString);

        builder.UseMySql(connectionString, serverVersion);
    }

    public override Task<IReadOnlyList<string>> GetDatabasesAsync(IDataConnectionPasswordProtector passwordProtector)
        => MariaDbUtils.GetDatabasesAsync(this, passwordProtector);
}
