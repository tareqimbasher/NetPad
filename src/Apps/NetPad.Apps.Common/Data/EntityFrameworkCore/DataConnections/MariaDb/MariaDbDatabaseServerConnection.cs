using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using NetPad.Data;
using NetPad.Data.Security;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections.MariaDb;

public sealed class MariaDbDatabaseServerConnection(Guid id, string name)
    : EntityFrameworkDatabaseServerConnection(id, name, DataConnectionType.MariaDB), IMariaDbConnection
{
    public override string GetConnectionString(IDataConnectionPasswordProtector passwordProtector)
        => MariaDbUtils.GetConnectionString(this, null, passwordProtector);

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
