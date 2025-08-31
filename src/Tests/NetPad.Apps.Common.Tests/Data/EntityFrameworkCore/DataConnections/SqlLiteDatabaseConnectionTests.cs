using NetPad.Apps.Data.EntityFrameworkCore.DataConnections;
using NetPad.Data;

namespace NetPad.Apps.Common.Tests.Data.EntityFrameworkCore.DataConnections;

public class SqlLiteDatabaseConnectionTests() : CommonTests(DataConnectionType.SQLite, SQLiteDatabaseConnection.ProviderName)
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
        ["host", "port", "db name", "user id", "password", null, "Data Source=db name;Password=password;"],
        [null, "port", "db name", "user id", "password", null, "Data Source=db name;Password=password;"],
        ["host", null, "db name", "user id", "password", null, "Data Source=db name;Password=password;"],
        ["host", "port", null, "user id", "password", null, "Data Source=;Password=password;"],
        ["host", "port", "db name", null, "password", null, "Data Source=db name;Password=password;"],
        ["host", "port", "db name", "user id", null, null, "Data Source=db name;"],
        ["host", "port", "db name", null, null, null, "Data Source=db name;"],
        [null, null, null, null, null, null, "Data Source=;"],
        [null, null, "/path/to/db.sqlite", null, null, null, "Data Source=/path/to/db.sqlite;"],
        [null, null, "/path/to/db.sqlite", null, "password", null, "Data Source=/path/to/db.sqlite;Password=password;"],
        ["host", "port", "db name", null, null, "Data Source=new host:new port", "Data Source=new host:new port;"],
        new[] { "host", "port", "db name", null, null, "Data Source=new host;Command Timeout=300", "Data Source=new host;Command Timeout=300;" },
    };

    protected override EntityFrameworkDatabaseConnection CreateConnection()
    {
        return new SQLiteDatabaseConnection(Guid.NewGuid(), "name");
    }
}
