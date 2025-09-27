using NetPad.Apps.Data.EntityFrameworkCore.DataConnections;
using NetPad.Data;

namespace NetPad.Apps.Common.Tests.Data.EntityFrameworkCore.DataConnections;

public class MsSqlServerDatabaseConnectionTests() : CommonTests(DataConnectionType.MSSQLServer, MsSqlServerDatabaseConnection.ProviderName)
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
        ["host", "port", "db name", "user id", "password", null, "Data Source=host,port;Initial Catalog=db name;User Id=user id;Password=password;"],
        [null, "port", "db name", "user id", "password", null, "Data Source=,port;Initial Catalog=db name;User Id=user id;Password=password;"],
        ["host", null, "db name", "user id", "password", null, "Data Source=host;Initial Catalog=db name;User Id=user id;Password=password;"],
        ["host", "port", null, "user id", "password", null, "Data Source=host,port;Initial Catalog=;User Id=user id;Password=password;"],
        ["host", "port", "db name", null, "password", null, "Data Source=host,port;Initial Catalog=db name;Password=password;"],
        ["host", "port", "db name", "user id", null, null, "Data Source=host,port;Initial Catalog=db name;User Id=user id;"],
        ["host", "port", "db name", null, null, null, "Data Source=host,port;Initial Catalog=db name;"],
        ["host", "port", "db name", null, null, "Initial Catalog=new db", "Data Source=host,port;Initial Catalog=new db;"],
        new[] { "host", "port", "db name", null, null, "Trust Server Certificate=True;MultipleActiveResultSets=True;", "Data Source=host,port;Initial Catalog=db name;Trust Server Certificate=True;MultipleActiveResultSets=True;" }
    };

    protected override EntityFrameworkDatabaseConnection CreateConnection()
    {
        return new MsSqlServerDatabaseConnection(Guid.NewGuid(), "name");
    }
}
