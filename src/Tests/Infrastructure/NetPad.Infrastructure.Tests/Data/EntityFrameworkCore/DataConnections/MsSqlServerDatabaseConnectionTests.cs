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
        new[] { "host", "port", "db name", "user id", "password", null, "Data Source=host,port;Initial Catalog=db name;User Id=user id;Password=password;" },
        new[] { null, "port", "db name", "user id", "password", null, "Data Source=,port;Initial Catalog=db name;User Id=user id;Password=password;" },
        new[] { "host", null, "db name", "user id", "password", null, "Data Source=host;Initial Catalog=db name;User Id=user id;Password=password;" },
        new[] { "host", "port", null, "user id", "password", null, "Data Source=host,port;Initial Catalog=;User Id=user id;Password=password;" },
        new[] { "host", "port", "db name", null, "password", null, "Data Source=host,port;Initial Catalog=db name;Password=password;" },
        new[] { "host", "port", "db name", "user id", null, null, "Data Source=host,port;Initial Catalog=db name;User Id=user id;" },
        new[] { "host", "port", "db name", null, null, null, "Data Source=host,port;Initial Catalog=db name;" },
        new[] { "host", "port", "db name", null, null, "Initial Catalog=new db", "Data Source=host,port;Initial Catalog=new db;" },
        new[] { "host", "port", "db name", null, null, "Trust Server Certificate=True;MultipleActiveResultSets=True;", "Data Source=host,port;Initial Catalog=db name;Trust Server Certificate=True;MultipleActiveResultSets=True;" }
    };

    protected override EntityFrameworkDatabaseConnection CreateConnection()
    {
        return new MsSqlServerDatabaseConnection(Guid.NewGuid(), "name");
    }
}
