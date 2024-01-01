using System;
using System.Collections.Generic;
using NetPad.Data;
using NetPad.Data.EntityFrameworkCore.DataConnections;
using Xunit;

namespace NetPad.Infrastructure.Tests.Data.EntityFrameworkCore.DataConnections;

public class SqlLiteDatabaseConnectionTests : CommonTests
{
    public SqlLiteDatabaseConnectionTests() : base(DataConnectionType.SQLite, "Microsoft.EntityFrameworkCore.Sqlite")
    {
    }

    [Theory]
    [MemberData(nameof(ConnectionStringTestData))]
    public void GetConnectionString_ShouldReturnCorrectlyFormattedConnectionString(
        string? host,
        string? port,
        string? databaseName,
        string? userId,
        string? password,
        string? connectionStringAugment,
        string expected
    )
    {
        var connection = CreateConnection();

        connection.Host = host;
        connection.Port = port;
        connection.DatabaseName = databaseName;
        connection.UserId = userId;
        connection.Password = password;
        connection.ConnectionStringAugment = connectionStringAugment;

        var connectionString = connection.GetConnectionString(new NullDataConnectionPasswordProtector());

        Assert.Equal(expected, connectionString);
    }

    public static IEnumerable<object?[]> ConnectionStringTestData => new[]
    {
        new[] { "host", "port", "db name", "user id", "password", null, "Data Source=db name;Password=password;" },
        new[] { null, "port", "db name", "user id", "password", null, "Data Source=db name;Password=password;" },
        new[] { "host", null, "db name", "user id", "password", null, "Data Source=db name;Password=password;" },
        new[] { "host", "port", null, "user id", "password", null, "Data Source=;Password=password;" },
        new[] { "host", "port", "db name", null, "password", null, "Data Source=db name;Password=password;" },
        new[] { "host", "port", "db name", "user id", null, null, "Data Source=db name;" },
        new[] { "host", "port", "db name", null, null, null, "Data Source=db name;" },
        new[] { null, null, null, null, null, null, "Data Source=;" },
        new[] { null, null, "/path/to/db.sqlite", null, null, null, "Data Source=/path/to/db.sqlite;" },
        new[] { null, null, "/path/to/db.sqlite", null, "password", null, "Data Source=/path/to/db.sqlite;Password=password;" },
        new[] { "host", "port", "db name", null, null, "Data Source=new host:new port", "Data Source=new host:new port;" },
        new[] { "host", "port", "db name", null, null, "Data Source=new host;Command Timeout=300", "Data Source=new host;Command Timeout=300;" },
    };

    protected override EntityFrameworkDatabaseConnection CreateConnection()
    {
        return new SQLiteDatabaseConnection(Guid.NewGuid(), "name");
    }
}
