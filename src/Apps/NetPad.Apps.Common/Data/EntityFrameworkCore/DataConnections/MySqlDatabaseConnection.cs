using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;
using NetPad.Data;
using NetPad.Data.Security;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections;

public sealed class MySqlDatabaseConnection(Guid id, string name, ScaffoldOptions? scaffoldOptions = null)
    : EntityFrameworkDatabaseConnection(id, name, DataConnectionType.MySQL, ProviderName, scaffoldOptions)
{
    public const string ProviderName = "Pomelo.EntityFrameworkCore.MySql";

    public override string GetConnectionString(IDataConnectionPasswordProtector passwordProtector)
    {
        ConnectionStringBuilder connectionStringBuilder = [];

        string host = Host ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(Port))
        {
            host += $";Port={Port}";
        }

        connectionStringBuilder.TryAdd("Server", host);
        connectionStringBuilder.TryAdd("Database", DatabaseName);

        if (UserId != null)
        {
            connectionStringBuilder.TryAdd("Uid", UserId);
        }

        if (Password != null)
        {
            connectionStringBuilder.TryAdd("Pwd", passwordProtector.Unprotect(Password));
        }

        if (!string.IsNullOrWhiteSpace(ConnectionStringAugment))
        {
            connectionStringBuilder.Augment(new ConnectionStringBuilder(ConnectionStringAugment));
        }

        return connectionStringBuilder.Build();
    }

    public override void ConfigureDbContextOptions(
        DbContextOptionsBuilder builder,
        IDataConnectionPasswordProtector passwordProtector)
    {
        var connectionString = GetConnectionString(passwordProtector);

        var serverVersion = MySqlServerVersion.AutoDetect(connectionString);

        builder.UseMySql(connectionString, serverVersion);
    }

    public override async Task<IReadOnlyList<string>> GetDatabasesAsync(
        IDataConnectionPasswordProtector passwordProtector)
    {
        await using DatabaseContext context = DatabaseContext.Create(this, passwordProtector);
        await using DbCommand command = context.Database.GetDbConnection().CreateCommand();

        command.CommandText = "select schema_name from information_schema.schemata;";
        await context.Database.OpenConnectionAsync();

        await using DbDataReader result = await command.ExecuteReaderAsync();

        List<string> databases = [];

        while (await result.ReadAsync())
        {
            databases.Add((string)result["schema_name"]);
        }

        return databases;
    }
}
