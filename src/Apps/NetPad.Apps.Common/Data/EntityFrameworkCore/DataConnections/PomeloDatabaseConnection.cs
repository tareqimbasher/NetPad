using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using NetPad.Data;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections;

internal class PomeloDatabaseConnection(
    Func<(string? host, string? port, string? databaseName, string? userId, string? password, string? connectionStringAugment)> getConnectionDetails)
{
    private readonly Func<(string? host, string? port, string? databaseName, string? userId, string? password, string? connectionStringAugment)> _getConnectionDetails = getConnectionDetails;

    internal string GetConnectionString(IDataConnectionPasswordProtector passwordProtector)
    {
        var (host, port, databaseName, userId, password, connectionStringAugment) = _getConnectionDetails();

        ConnectionStringBuilder connectionStringBuilder = [];

        host ??= string.Empty;

        if (!string.IsNullOrWhiteSpace(port))
        {
            host += $";Port={port}";
        }

        connectionStringBuilder.TryAdd("Server", host);
        connectionStringBuilder.TryAdd("Database", databaseName);

        if (userId != null)
        {
            connectionStringBuilder.TryAdd("Uid", userId);
        }

        if (password != null)
        {
            connectionStringBuilder.TryAdd("Pwd", passwordProtector.Unprotect(password));
        }

        if (!string.IsNullOrWhiteSpace(connectionStringAugment))
        {
            connectionStringBuilder.Augment(new ConnectionStringBuilder(connectionStringAugment));
        }

        return connectionStringBuilder.Build();
    }

    internal Task ConfigureDbContextOptionsAsync(DbContextOptionsBuilder builder, IDataConnectionPasswordProtector passwordProtector)
    {
        var connectionString = GetConnectionString(passwordProtector);

        var serverVersion = ServerVersion.AutoDetect(connectionString);

        builder.UseMySql(connectionString, serverVersion, options =>
        {
            options.EnableRetryOnFailure();
        });

        return Task.CompletedTask;
    }

    internal async Task<IEnumerable<string>> GetDatabasesAsync(IDataConnectionPasswordProtector passwordProtector, DatabaseContext context)
    {
        await using DbCommand command = context.Database.GetDbConnection().CreateCommand();

        command.CommandText = "SELECT schema_name FROM information_schema.schemata;";
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