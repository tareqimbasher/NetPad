using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace NetPad.Data.EntityFrameworkCore.DataConnections;

public sealed class PostgreSqlDatabaseConnection : EntityFrameworkRelationalDatabaseConnection
{
    public PostgreSqlDatabaseConnection(Guid id, string name)
        : base(id, name, DataConnectionType.PostgreSQL, "Npgsql.EntityFrameworkCore.PostgreSQL")
    {
    }

    public override string GetConnectionString()
    {
        var connectionString = $"Host={Host}";
        if (Port != null)
        {
            connectionString += $":{Port}";
        }

        connectionString += $";Database={DatabaseName}";

        if (UserId != null || Password != null)
        {
            connectionString += $";UserId={UserId};Password={Password}";
        }

        return connectionString;
    }

    public override Task ConfigureDbContextOptionsAsync(DbContextOptionsBuilder builder)
    {
        builder.UseNpgsql(GetConnectionString());
        return Task.CompletedTask;
    }

    public override async Task<IEnumerable<string>> GetDatabasesAsync()
    {
        await using var context = CreateDbContext();
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
