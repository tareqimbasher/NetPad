using Microsoft.EntityFrameworkCore;
using NetPad.Data;
using NetPad.Data.Security;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections;

public sealed class MsSqlServerDatabaseServerConnection(Guid id, string name)
    : EntityFrameworkDatabaseServerConnection(id, name, DataConnectionType.MSSQLServer)
{
    public override string GetConnectionString(IDataConnectionPasswordProtector passwordProtector)
    {
        var connectionStringBuilder = new ConnectionStringBuilder();

        string dataSource = Host ?? "";
        if (!string.IsNullOrWhiteSpace(Port))
        {
            dataSource += $",{Port}";
        }

        connectionStringBuilder.TryAdd("Data Source", dataSource);
        //connectionStringBuilder.TryAdd("Initial Catalog", "master");

        if (UserId != null)
        {
            connectionStringBuilder.TryAdd("User Id", UserId);
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
        builder.UseSqlServer(GetConnectionString(passwordProtector));
    }

    public override async Task<IEnumerable<string>> GetDatabasesAsync(IDataConnectionPasswordProtector passwordProtector)
    {
        await using var context = DatabaseContext.Create(this, passwordProtector);
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
