using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace NetPad.Data.EntityFrameworkCore.DataConnections;

public sealed class PostgreSqlDatabaseConnection : EntityFrameworkRelationalDatabaseConnection
{
    public PostgreSqlDatabaseConnection(Guid id, string name)
        : base(id, name, DataConnectionType.PostgreSQL, "Npgsql.EntityFrameworkCore.PostgreSQL")
    {
    }

    public override string GetConnectionString(IDataConnectionPasswordProtector passwordProtector)
    {
        var connectionString = new StringBuilder($"Host={Host}");
        if (!string.IsNullOrWhiteSpace(Port))
        {
            connectionString.Append($":{Port}");
        }

        connectionString.Append($";Database={DatabaseName}");

        if (UserId != null)
        {
            connectionString.Append($";UserId={UserId}");
        }

        if (Password != null)
        {
            connectionString.Append($";Password={passwordProtector.Unprotect(Password)}");
        }

        return connectionString.ToString();
    }

    public override Task ConfigureDbContextOptionsAsync(DbContextOptionsBuilder builder, IDataConnectionPasswordProtector passwordProtector)
    {
        builder.UseNpgsql(GetConnectionString(passwordProtector));
        return Task.CompletedTask;
    }

    public override async Task<IEnumerable<string>> GetDatabasesAsync(IDataConnectionPasswordProtector passwordProtector)
    {
        await using var context = CreateDbContext(passwordProtector);
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
