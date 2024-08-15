using NetPad.Apps.Data.EntityFrameworkCore.DataConnections;
using NetPad.Data;

namespace NetPad.Apps.Common.Tests.Data.EntityFrameworkCore.DataConnections;

public class MySqlDatabaseConnectionTests() : CommonTests(DataConnectionType.MySQL, "Pomelo.EntityFrameworkCore.MySql")
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

    public static IEnumerable<object[]> ConnectionStringTestData // TODO: nyvall Add more test data
    {
        get
        {
            yield return new object[] { "localhost", "1234", "db name", "user id", "password", null!, "Server=localhost;Port=1234;Database=db name;Uid=user id;Pwd=password;" };
        }
    }

    protected override EntityFrameworkDatabaseConnection CreateConnection()
    {
        return new MySqlDatabaseConnection(Guid.NewGuid(), "name");
    }
}