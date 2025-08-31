using NetPad.Apps.Data.EntityFrameworkCore.DataConnections;
using NetPad.Data;

namespace NetPad.Apps.Common.Tests.Data.EntityFrameworkCore.DataConnections;

public class MySqlDatabaseConnectionTests() : CommonTests(DataConnectionType.MySQL, MySqlDatabaseConnection.ProviderName)
{
    [Theory]
    [MemberData(nameof(ConnectionStringTestData))]
    public void GetConnectionString_ShouldReturnCorrectlyFormattedConnectionString(
        string? server,
        string? port,
        string? databaseName,
        string? userId,
        string? password,
        string? connectionStringAugment,
        string expected
    )
    {
        var connection = CreateConnection();

        connection.Host = server;
        connection.Port = port;
        connection.DatabaseName = databaseName;
        connection.UserId = userId;
        connection.Password = password;
        connection.ConnectionStringAugment = connectionStringAugment;

        var connectionString = connection.GetConnectionString(new NullDataConnectionPasswordProtector());

        Assert.Equal(expected, connectionString);
    }

    public static IEnumerable<object?[]> ConnectionStringTestData =>
    [
        ["localhost", "port", "db name", "user id", "password", null, "Server=localhost;Port=port;Database=db name;Uid=user id;Pwd=password;"],
        [null, "port", "db name", "user id", "password", null, "Server=;Port=port;Database=db name;Uid=user id;Pwd=password;"],
        ["localhost", null, "db name", "user id", "password", null, "Server=localhost;Database=db name;Uid=user id;Pwd=password;"],
        ["localhost", "port", null, "user id", "password", null, "Server=localhost;Port=port;Database=;Uid=user id;Pwd=password;"],
        ["localhost", "port", "db name", null, "password", null, "Server=localhost;Port=port;Database=db name;Pwd=password;"],
        ["localhost", "port", "db name", "user id", null, null, "Server=localhost;Port=port;Database=db name;Uid=user id;"],
        ["localhost", "port", "db name", null, null, null, "Server=localhost;Port=port;Database=db name;"],
        ["localhost", "port", "db name", null, null, "Server=new host:new port", "Server=new host:new port;Database=db name;"],
        new[] { "localhost", "port", "db name", null, null, "Server=new host;Command Timeout=300", "Server=new host;Database=db name;Command Timeout=300;" },
    ];

    protected override EntityFrameworkDatabaseConnection CreateConnection()
    {
        return new MySqlDatabaseConnection(Guid.NewGuid(), "name");
    }
}