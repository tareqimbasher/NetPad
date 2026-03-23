using Microsoft.EntityFrameworkCore;
using NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;
using NetPad.Data;
using NetPad.Data.Security;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections.MySql;

public sealed class MySqlDatabaseConnection(Guid id, string name, ScaffoldOptions? scaffoldOptions = null)
    : EntityFrameworkDatabaseConnection(id, name, DataConnectionType.MySQL, ProviderName, scaffoldOptions),
        IMySqlConnection
{
    public const string ProviderName = "Pomelo.EntityFrameworkCore.MySql";

    public override string GetConnectionString(IDataConnectionPasswordProtector passwordProtector)
        => MySqlUtils.GetConnectionString(this, DatabaseName, passwordProtector);

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
