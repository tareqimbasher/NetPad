using Microsoft.EntityFrameworkCore;
using NetPad.Data.Security;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections.MsSqlServer;

public static class MsSqlServerUtils
{
    public static string GetConnectionString(
        IMsSqlServerConnection connection,
        string? databaseName,
        IDataConnectionPasswordProtector passwordProtector)
    {
        var connectionStringBuilder = new ConnectionStringBuilder();

        string dataSource = connection.Host ?? "";
        if (!string.IsNullOrWhiteSpace(connection.Port))
        {
            dataSource += $",{connection.Port}";
        }

        connectionStringBuilder.TryAdd("Data Source", dataSource);

        if (!string.IsNullOrWhiteSpace(databaseName))
        {
            connectionStringBuilder.TryAdd("Initial Catalog", databaseName);
        }

        if (connection.UserId != null)
        {
            connectionStringBuilder.TryAdd("User Id", connection.UserId);
        }

        if (connection.Password != null)
        {
            connectionStringBuilder.TryAdd("Password", passwordProtector.Unprotect(connection.Password));
        }

        if (!string.IsNullOrWhiteSpace(connection.ConnectionStringAugment))
        {
            connectionStringBuilder.Augment(new ConnectionStringBuilder(connection.ConnectionStringAugment));
        }

        return connectionStringBuilder.Build();
    }

    public static async Task<IReadOnlyList<string>> GetDatabasesAsync(
        IMsSqlServerConnection connection,
        IDataConnectionPasswordProtector passwordProtector)
    {
        await using var context = DatabaseContext.Create(connection, passwordProtector);
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
