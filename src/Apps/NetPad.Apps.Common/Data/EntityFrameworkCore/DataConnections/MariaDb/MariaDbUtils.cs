using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using NetPad.Data.Security;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections.MariaDb;

public static class MariaDbUtils
{
    public static string GetConnectionString(
        IMariaDbConnection connection,
        string? databaseName,
        IDataConnectionPasswordProtector passwordProtector)
    {
        ConnectionStringBuilder connectionStringBuilder = [];

        string host = connection.Host ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(connection.Port))
        {
            host += $";Port={connection.Port}";
        }

        connectionStringBuilder.TryAdd("Server", host);

        if (!string.IsNullOrWhiteSpace(databaseName))
        {
            connectionStringBuilder.TryAdd("Database", databaseName);
        }

        if (connection.UserId != null)
        {
            connectionStringBuilder.TryAdd("Uid", connection.UserId);
        }

        if (connection.Password != null)
        {
            connectionStringBuilder.TryAdd("Pwd", passwordProtector.Unprotect(connection.Password));
        }

        if (!string.IsNullOrWhiteSpace(connection.ConnectionStringAugment))
        {
            connectionStringBuilder.Augment(new ConnectionStringBuilder(connection.ConnectionStringAugment));
        }

        return connectionStringBuilder.Build();
    }

    public static async Task<IReadOnlyList<string>> GetDatabasesAsync(
        IMariaDbConnection connection,
        IDataConnectionPasswordProtector passwordProtector)
    {
        await using DatabaseContext context = DatabaseContext.Create(connection, passwordProtector);
        await using DbCommand command = context.Database.GetDbConnection().CreateCommand();

        command.CommandText = "select schema_name from information_schema.schemata;";
        await context.Database.OpenConnectionAsync();

        await using DbDataReader result = await command.ExecuteReaderAsync();

        List<string> databases = [];

        while (await result.ReadAsync())
        {
            databases.Add((string)result["schema_name"]);
        }

        return databases;
    }
}
