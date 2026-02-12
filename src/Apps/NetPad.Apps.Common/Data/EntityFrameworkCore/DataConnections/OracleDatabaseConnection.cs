using Microsoft.EntityFrameworkCore;
using NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;
using NetPad.Data;
using NetPad.Data.Security;

namespace NetPad.Apps.Data.EntityFrameworkCore.DataConnections;

public sealed class OracleDatabaseConnection(Guid id, string name, ScaffoldOptions? scaffoldOptions = null)
    : EntityFrameworkDatabaseConnection(id, name, DataConnectionType.Oracle, ProviderName,
        scaffoldOptions)
{
    public const string ProviderName = "Oracle.EntityFrameworkCore";

    public override string GetConnectionString(IDataConnectionPasswordProtector passwordProtector)
    {
        // It builds a connection string with 'Easy Connect Naming Method' Oracle format
        var connectionStringBuilder = new ConnectionStringBuilder();
        var dataSource = string.Empty;

        if (!string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(DatabaseName))
        {
            string port = Port ?? "1521";
            dataSource = $"//{Host}:{port}/{DatabaseName}";
        }

        connectionStringBuilder.TryAdd("Data Source", dataSource);

        if (UserId != null)
        {
            connectionStringBuilder.TryAdd("User Id", UserId);
        }

        if (Password != null)
        {
            connectionStringBuilder.TryAdd("Password", passwordProtector.Unprotect(Password));
        }

        if (!string.IsNullOrWhiteSpace(ConnectionStringAugment))
        {
            connectionStringBuilder.Augment(new ConnectionStringBuilder(ConnectionStringAugment));
        }

        return connectionStringBuilder.Build();
    }

    public override void ConfigureDbContextOptions(
        DbContextOptionsBuilder builder,
        IDataConnectionPasswordProtector passwordProtector)
    {
        builder.UseOracle(GetConnectionString(passwordProtector));
    }

    public override Task<IReadOnlyList<string>> GetDatabasesAsync(IDataConnectionPasswordProtector passwordProtector)
    {
        return Task.FromResult<IReadOnlyList<string>>([]);
    }
}
