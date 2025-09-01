using NetPad.Apps.Data.EntityFrameworkCore.DataConnections;
using NetPad.Data;

namespace NetPad.Apps.Common.Tests.Data.EntityFrameworkCore.DataConnections;

public class OracleDatabaseConnectionTests() : CommonTests(DataConnectionType.Oracle, OracleDatabaseConnection.ProviderName)
{
    public static IEnumerable<object?[]> ConnectionStringTestData =>
    [
        ["localhost", "1521", "XE", "scott_tiger", "Pw123!", null, "Data Source=//localhost:1521/XE;User Id=scott_tiger;Password=Pw123!;"],
        ["localhost", null, "XE", "scott_tiger", "Pw123!", null, "Data Source=//localhost:1521/XE;User Id=scott_tiger;Password=Pw123!;"],
    ];

    [Theory]
    [MemberData(nameof(ConnectionStringTestData))]
    public void GetConnectionString_ShouldReturnCorrectlyFormattedConnectionString(
        string? host,
        string? port,
        string? serviceName,
        string? userId,
        string? password,
        string? connectionStringAugment,
        string expected
    )
    {
        var connection = CreateConnection();

        connection.Host = host;
        connection.Port = port;
        connection.DatabaseName = serviceName;
        connection.UserId = userId;
        connection.Password = password;
        connection.ConnectionStringAugment = connectionStringAugment;

        var connectionString = connection.GetConnectionString(new NullDataConnectionPasswordProtector());

        Assert.Equal(expected, connectionString);
    }
    protected override EntityFrameworkDatabaseConnection CreateConnection()
    {
        return new OracleDatabaseConnection(Guid.NewGuid(), "name");
    }
}
