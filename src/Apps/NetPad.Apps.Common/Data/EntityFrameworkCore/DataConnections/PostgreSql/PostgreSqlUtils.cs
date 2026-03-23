using Microsoft.EntityFrameworkCore;
using NetPad.Data.Security;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections.PostgreSql;

public static class PostgreSqlUtils
{
    public static string GetConnectionString(
        IPostgreSqlConnection connection,
        string? databaseName,
        IDataConnectionPasswordProtector passwordProtector)
    {
        var connectionStringBuilder = new ConnectionStringBuilder();

        var host = connection.Host ?? "";
        if (!string.IsNullOrWhiteSpace(connection.Port))
        {
            host += $":{connection.Port}";
        }

        connectionStringBuilder.TryAdd("Host", host);

        if (!string.IsNullOrWhiteSpace(databaseName))
        {
            connectionStringBuilder.TryAdd("Database", databaseName);
        }

        if (connection.UserId != null)
        {
            connectionStringBuilder.TryAdd("UserId", connection.UserId);
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
        IPostgreSqlConnection connection,
        IDataConnectionPasswordProtector passwordProtector)
    {
        await using var context = DatabaseContext.Create(connection, passwordProtector);
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
