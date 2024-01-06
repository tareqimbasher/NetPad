using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NetPad.Data.EntityFrameworkCore.Scaffolding;

namespace NetPad.Data.EntityFrameworkCore.DataConnections;

public sealed class MsSqlServerDatabaseConnection : EntityFrameworkRelationalDatabaseConnection
{
    public MsSqlServerDatabaseConnection(Guid id, string name, ScaffoldOptions? scaffoldOptions = null)
        : base(id, name, DataConnectionType.MSSQLServer, "Microsoft.EntityFrameworkCore.SqlServer", scaffoldOptions)
    {
    }

    public override string GetConnectionString(IDataConnectionPasswordProtector passwordProtector)
    {
        var connectionStringBuilder = new ConnectionStringBuilder();

        string dataSource = Host ?? "";
        if (!string.IsNullOrWhiteSpace(Port))
        {
            dataSource += $",{Port}";
        }

        connectionStringBuilder.TryAdd("Data Source", dataSource);
        connectionStringBuilder.TryAdd("Initial Catalog", DatabaseName);

        if (UserId != null)
        {
            connectionStringBuilder.TryAdd("User Id", UserId);
        }

        if (Password != null)
        {
            connectionStringBuilder.TryAdd("Password", passwordProtector.Unprotect(Password));
        }

        if (!string.IsNullOrWhiteSpace(ConnectionStringAugment))
            connectionStringBuilder.Augment(new ConnectionStringBuilder(ConnectionStringAugment));

        return connectionStringBuilder.Build();
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
