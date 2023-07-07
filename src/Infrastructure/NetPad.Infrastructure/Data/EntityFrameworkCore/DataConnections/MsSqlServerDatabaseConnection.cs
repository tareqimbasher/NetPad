using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace NetPad.Data.EntityFrameworkCore.DataConnections;

public sealed class MsSqlServerDatabaseConnection : EntityFrameworkRelationalDatabaseConnection
{
    public MsSqlServerDatabaseConnection(Guid id, string name)
        : base(id, name, DataConnectionType.MSSQLServer, "Microsoft.EntityFrameworkCore.SqlServer")
    {
    }

    public override string GetConnectionString(IDataConnectionPasswordProtector passwordProtector)
    {
        var connectionString = $"Data Source={Host}";
        if (!string.IsNullOrWhiteSpace(Port))
        {
            connectionString += $",{Port}";
        }

        connectionString += $";Initial Catalog={DatabaseName}";

        if (UserId != null)
        {
            connectionString += $";User Id={UserId}";
        }

        if (Password != null)
        {
            connectionString += $";Password={passwordProtector.Unprotect(Password)}";
        }

        connectionString += ";Trust Server Certificate=True";

        return connectionString;
    }

    public override Task ConfigureDbContextOptionsAsync(DbContextOptionsBuilder builder, IDataConnectionPasswordProtector passwordProtector)
    {
        builder.UseSqlServer(GetConnectionString(passwordProtector));
        return Task.CompletedTask;
    }

    public override async Task<IEnumerable<string>> GetDatabasesAsync(IDataConnectionPasswordProtector passwordProtector)
    {
        await using var context = CreateDbContext(passwordProtector);
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
