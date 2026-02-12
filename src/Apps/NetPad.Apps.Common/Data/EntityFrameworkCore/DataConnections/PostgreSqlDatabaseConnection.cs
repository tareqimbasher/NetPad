using Microsoft.EntityFrameworkCore;
using NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;
using NetPad.Data;
using NetPad.Data.Security;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections;

public sealed class PostgreSqlDatabaseConnection(Guid id, string name, ScaffoldOptions? scaffoldOptions = null)
    : EntityFrameworkDatabaseConnection(id, name, DataConnectionType.PostgreSQL,
        ProviderName, scaffoldOptions)
{
    public const string ProviderName = "Npgsql.EntityFrameworkCore.PostgreSQL";

    public override string GetConnectionString(IDataConnectionPasswordProtector passwordProtector)
    {
        var connectionStringBuilder = new ConnectionStringBuilder();

        string host = Host ?? "";
        if (!string.IsNullOrWhiteSpace(Port))
        {
            host += $":{Port}";
        }

        connectionStringBuilder.TryAdd("Host", host);
        connectionStringBuilder.TryAdd("Database", DatabaseName);

        if (UserId != null)
        {
            connectionStringBuilder.TryAdd("UserId", UserId);
        }

        if (Password != null)
        {
            connectionStringBuilder.TryAdd("Password", passwordProtector.Unprotect(Password));
        }

        if (!string.IsNullOrWhiteSpace(ConnectionStringAugment))
            connectionStringBuilder.Augment(new ConnectionStringBuilder(ConnectionStringAugment));

        return connectionStringBuilder.Build();
    }

    public override void ConfigureDbContextOptions(
        DbContextOptionsBuilder builder,
        IDataConnectionPasswordProtector passwordProtector)
    {
        builder.UseNpgsql(GetConnectionString(passwordProtector));
    }

    public override async Task<IReadOnlyList<string>> GetDatabasesAsync(IDataConnectionPasswordProtector passwordProtector)
    {
        await using var context = DatabaseContext.Create(this, passwordProtector);
        await using var command = context.Database.GetDbConnection().CreateCommand();

        command.CommandText = "select datname from pg_database;";
        await context.Database.OpenConnectionAsync();

        await using var result = await command.ExecuteReaderAsync();

        var databases = new List<string>();
        while (await result.ReadAsync())
        {
            databases.Add((string)result["datname"]);
        }

        return databases;
    }
}
