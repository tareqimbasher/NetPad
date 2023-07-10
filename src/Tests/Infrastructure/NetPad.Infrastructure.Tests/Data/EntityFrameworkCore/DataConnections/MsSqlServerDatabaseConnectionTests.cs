using System;
using System.Collections.Generic;
using NetPad.Data;
using NetPad.Data.EntityFrameworkCore.DataConnections;
using Xunit;

namespace NetPad.Infrastructure.Tests.Data.EntityFrameworkCore.DataConnections;

public class MsSqlServerDatabaseConnectionTests : CommonTests
{
    public MsSqlServerDatabaseConnectionTests() : base(DataConnectionType.MSSQLServer, "Microsoft.EntityFrameworkCore.SqlServer")
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
        new[] { "host", "port", "db name", "user id", "password", "Data Source=host,port;Initial Catalog=db name;User Id=user id;Password=password;Trust Server Certificate=True;MultipleActiveResultSets=True;" },
        new[] { null, "port", "db name", "user id", "password", "Data Source=,port;Initial Catalog=db name;User Id=user id;Password=password;Trust Server Certificate=True;MultipleActiveResultSets=True;" },
        new[] { "host", null, "db name", "user id", "password", "Data Source=host;Initial Catalog=db name;User Id=user id;Password=password;Trust Server Certificate=True;MultipleActiveResultSets=True;" },
        new[] { "host", "port", null, "user id", "password", "Data Source=host,port;Initial Catalog=;User Id=user id;Password=password;Trust Server Certificate=True;MultipleActiveResultSets=True;" },
        new[] { "host", "port", "db name", null, "password", "Data Source=host,port;Initial Catalog=db name;Password=password;Trust Server Certificate=True;MultipleActiveResultSets=True;" },
        new[] { "host", "port", "db name", "user id", null, "Data Source=host,port;Initial Catalog=db name;User Id=user id;Trust Server Certificate=True;MultipleActiveResultSets=True;" },
        new[] { "host", "port", "db name", null, null, "Data Source=host,port;Initial Catalog=db name;Trust Server Certificate=True;MultipleActiveResultSets=True;" }
    };

    protected override EntityFrameworkDatabaseConnection CreateConnection()
    {
        return new MsSqlServerDatabaseConnection(Guid.NewGuid(), "name");
    }
}
