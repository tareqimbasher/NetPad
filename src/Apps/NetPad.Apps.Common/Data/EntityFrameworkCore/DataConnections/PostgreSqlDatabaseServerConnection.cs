using Microsoft.EntityFrameworkCore;
using NetPad.Data;
using NetPad.Data.Security;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections;

public sealed class PostgreSqlDatabaseServerConnection(Guid id, string name)
    : EntityFrameworkDatabaseServerConnection(id, name, DataConnectionType.PostgreSQL)
{
    public override string GetConnectionString(IDataConnectionPasswordProtector passwordProtector)
    {
        var connectionStringBuilder = new ConnectionStringBuilder();

        string host = Host ?? "";
        if (!string.IsNullOrWhiteSpace(Port))
        {
            host += $":{Port}";
        }

        connectionStringBuilder.TryAdd("Host", host);
        //connectionStringBuilder.TryAdd("Database", "postgres");

        if (UserId != null)
        {
            connectionStringBuilder.TryAdd("UserId", UserId);
        }

        if (Password != null)
        {
            connectionStringBuilder.TryAdd("Password", passwordProtector.Unprotect(Password));
        }

        return connectionStringBuilder.Build();
    }

    public override void ConfigureDbContextOptions(
        DbContextOptionsBuilder builder,
        IDataConnectionPasswordProtector passwordProtector)
    {
        builder.UseNpgsql(GetConnectionString(passwordProtector));
    }

    public override async Task<IEnumerable<string>> GetDatabasesAsync(IDataConnectionPasswordProtector passwordProtector)
    {
        await using var context = DatabaseContext.Create(this, passwordProtector);
        await using var command = context.Database.GetDbConnection().CreateCommand();

        command.CommandText = "select datname from pg_database where datistemplate = false;";
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
