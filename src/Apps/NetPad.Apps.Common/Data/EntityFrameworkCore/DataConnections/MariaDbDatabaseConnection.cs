using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;
using NetPad.Data;
using NetPad.Data.Security;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections;

public sealed class MariaDbDatabaseConnection(Guid id, string name, ScaffoldOptions? scaffoldOptions = null)
    : EntityFrameworkRelationalDatabaseConnection(id, name, DataConnectionType.MariaDB, "Pomelo.EntityFrameworkCore.MySql", scaffoldOptions)
{
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

    public override Task ConfigureDbContextOptionsAsync(DbContextOptionsBuilder builder, IDataConnectionPasswordProtector passwordProtector)
    {
        var connectionString = GetConnectionString(passwordProtector);

        var serverVersion = MariaDbServerVersion.AutoDetect(connectionString);

        builder.UseMySql(connectionString, serverVersion);

        return Task.CompletedTask;
    }

    public override async Task<IEnumerable<string>> GetDatabasesAsync(IDataConnectionPasswordProtector passwordProtector)
    {
        await using DatabaseContext context = CreateDbContext(passwordProtector);
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
