using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NetPad.Data.EntityFrameworkCore.Scaffolding;

namespace NetPad.Data.EntityFrameworkCore.DataConnections;

public sealed class SQLiteDatabaseConnection : EntityFrameworkRelationalDatabaseConnection
{
    public SQLiteDatabaseConnection(Guid id, string name, ScaffoldOptions? scaffoldOptions = null)
        : base(id, name, DataConnectionType.SQLite, "Microsoft.EntityFrameworkCore.Sqlite", scaffoldOptions)
    {
    }

    public override string GetConnectionString(IDataConnectionPasswordProtector passwordProtector)
    {
        var connectionStringBuilder = new ConnectionStringBuilder();

        connectionStringBuilder.TryAdd("Data Source", DatabaseName);

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
        builder.UseSqlite(GetConnectionString(passwordProtector));
        return Task.CompletedTask;
    }

    public override Task<IEnumerable<string>> GetDatabasesAsync(IDataConnectionPasswordProtector passwordProtector)
    {
        return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
    }
}
