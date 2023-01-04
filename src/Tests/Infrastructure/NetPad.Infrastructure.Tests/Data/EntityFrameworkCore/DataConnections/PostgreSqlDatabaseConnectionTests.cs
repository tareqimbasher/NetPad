using System;
using System.Collections.Generic;
using NetPad.Data;
using NetPad.Data.EntityFrameworkCore.DataConnections;
using Xunit;

namespace NetPad.Infrastructure.Tests.Data.EntityFrameworkCore.DataConnections;

public class PostgreSqlDatabaseConnectionTests : CommonTests
{
    public PostgreSqlDatabaseConnectionTests() : base(DataConnectionType.PostgreSQL, "Npgsql.EntityFrameworkCore.PostgreSQL")
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
        string expected
    )
    {
        var connection = CreateConnection();

        connection.Host = host;
        connection.Port = port;
        connection.DatabaseName = databaseName;
        connection.UserId = userId;
        connection.Password = password;

        Assert.Equal(expected, connection.GetConnectionString(new NullDataConnectionPasswordProtector()));
    }

    public static IEnumerable<object?[]> ConnectionStringTestData => new[]
    {
        new[] { "host", "port", "db name", "user id", "password", "Host=host:port;Database=db name;UserId=user id;Password=password" },
        new[] { null, "port", "db name", "user id", "password", "Host=:port;Database=db name;UserId=user id;Password=password" },
        new[] { "host", null, "db name", "user id", "password", "Host=host;Database=db name;UserId=user id;Password=password" },
        new[] { "host", "port", null, "user id", "password", "Host=host:port;Database=;UserId=user id;Password=password" },
        new[] { "host", "port", "db name", null, "password", "Host=host:port;Database=db name;Password=password" },
        new[] { "host", "port", "db name", "user id", null, "Host=host:port;Database=db name;UserId=user id" },
        new[] { "host", "port", "db name", null, null, "Host=host:port;Database=db name" }
    };

    protected override EntityFrameworkDatabaseConnection CreateConnection()
    {
        return new PostgreSqlDatabaseConnection(Guid.NewGuid(), "name");
    }
}
