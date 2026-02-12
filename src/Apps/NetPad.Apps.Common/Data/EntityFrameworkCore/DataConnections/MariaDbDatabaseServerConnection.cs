using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using NetPad.Data;
using NetPad.Data.Security;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections;

public sealed class MariaDbDatabaseServerConnection(Guid id, string name)
    : EntityFrameworkDatabaseServerConnection(id, name, DataConnectionType.MariaDB)
{
    public override string GetConnectionString(IDataConnectionPasswordProtector passwordProtector)
    {
        ConnectionStringBuilder connectionStringBuilder = [];

        string host = Host ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(Port))
        {
            host += $";Port={Port}";
        }

        connectionStringBuilder.TryAdd("Server", host);

        if (UserId != null)
        {
            connectionStringBuilder.TryAdd("Uid", UserId);
        }

        if (Password != null)
        {
            connectionStringBuilder.TryAdd("Pwd", passwordProtector.Unprotect(Password));
        }

        return connectionStringBuilder.Build();
    }

    public override void ConfigureDbContextOptions(
        DbContextOptionsBuilder builder,
        IDataConnectionPasswordProtector passwordProtector)
    {
        var connectionString = GetConnectionString(passwordProtector);
        var serverVersion = MariaDbServerVersion.AutoDetect(connectionString);
        builder.UseMySql(connectionString, serverVersion);
    }

    public override async Task<IEnumerable<string>> GetDatabasesAsync(IDataConnectionPasswordProtector passwordProtector)
    {
        await using DatabaseContext context = DatabaseContext.Create(this, passwordProtector);
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
