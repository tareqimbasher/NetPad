using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace NetPad.Data;

public class MsSqlServerDatabaseConnection : EntityFrameworkRelationalDatabaseConnection
{
    public MsSqlServerDatabaseConnection(Guid id, string name)
        : base(id, name, DataConnectionType.MSSQLServer, "Microsoft.EntityFrameworkCore.SqlServer")
    {
    }

    public override string GetConnectionString()
    {
        var connectionString = $"Data Source={Host}";
        if (Port != null)
        {
            connectionString += $",{Port}";
        }

        connectionString += $";Initial Catalog={DatabaseName}";

        if (UserId != null || Password != null)
        {
            connectionString += $";UserId={UserId};Password={Password}";
        }

        return connectionString;
    }

    public override Task ConfigureDbContextOptionsAsync(DbContextOptionsBuilder builder)
    {
        builder.UseSqlServer(GetConnectionString());
        return Task.CompletedTask;
    }

    public override async Task<IEnumerable<string>> GetDatabasesAsync()
    {
        await using var context = CreateDbContext();
        await using var command = context.Database.GetDbConnection().CreateCommand();

        command.CommandText = "SELECT name FROM master.dbo.sysdatabases;";
        await context.Database.OpenConnectionAsync();

        await using var result = await command.ExecuteReaderAsync();

        var databases = new List<string>();
        while (await result.ReadAsync())
        {
            databases.Add((string)result["name"]);
        }

        return databases;
    }
}
