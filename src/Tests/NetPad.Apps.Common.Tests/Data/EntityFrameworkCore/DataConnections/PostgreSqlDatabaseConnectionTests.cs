using NetPad.Apps.Data.EntityFrameworkCore.DataConnections;
using NetPad.Data;

namespace NetPad.Apps.Common.Tests.Data.EntityFrameworkCore.DataConnections;

public class PostgreSqlDatabaseConnectionTests() : CommonTests(DataConnectionType.PostgreSQL, "Npgsql.EntityFrameworkCore.PostgreSQL")
{
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
        ["host", "port", "db name", "user id", "password", null, "Host=host:port;Database=db name;UserId=user id;Password=password;"],
        [null, "port", "db name", "user id", "password", null, "Host=:port;Database=db name;UserId=user id;Password=password;"],
        ["host", null, "db name", "user id", "password", null, "Host=host;Database=db name;UserId=user id;Password=password;"],
        ["host", "port", null, "user id", "password", null, "Host=host:port;Database=;UserId=user id;Password=password;"],
        ["host", "port", "db name", null, "password", null, "Host=host:port;Database=db name;Password=password;"],
        ["host", "port", "db name", "user id", null, null, "Host=host:port;Database=db name;UserId=user id;"],
        ["host", "port", "db name", null, null, null, "Host=host:port;Database=db name;"],
        ["host", "port", "db name", null, null, "Host=new host:new port", "Host=new host:new port;Database=db name;"],
        new[] { "host", "port", "db name", null, null, "Host=new host;Command Timeout=300", "Host=new host;Database=db name;Command Timeout=300;" },

    };

    protected override EntityFrameworkDatabaseConnection CreateConnection()
    {
        return new PostgreSqlDatabaseConnection(Guid.NewGuid(), "name");
    }
}
