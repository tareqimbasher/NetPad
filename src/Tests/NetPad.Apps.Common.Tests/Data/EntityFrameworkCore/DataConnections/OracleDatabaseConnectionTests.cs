using NetPad.Apps.Data.EntityFrameworkCore.DataConnections;
using NetPad.Data;

namespace NetPad.Apps.Common.Tests.Data.EntityFrameworkCore.DataConnections;

public class OracleDatabaseConnectionTests() : CommonTests(DataConnectionType.Oracle, "Oracle.EntityFrameworkCore")
{
    public static IEnumerable<object?[]> ConnectionStringTestData =>
    [
        // TNS Alias Connection
        ["ORCL", "scott_tiger", "test123", null, "Data Source=ORCL;User Id=scott_tiger;Password=test123;"],
        ["ORCL", null, "test123", null, "Data Source=ORCL;Password=test123;"],
        ["ORCL", "scott_tiger", null, null, "Data Source=ORCL;User Id=scott_tiger;"],
        ["ORCL", null, null, null, "Data Source=ORCL;"],
        // Using the Connect Descriptor
        [
            null, "scott_tiger", "test123", "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=tcp)(HOST=sales-server)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=sales.us.acme.com)))",
            "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=tcp)(HOST=sales-server)(PORT=1521))(CONNECT_DATA=(SERVICE_NAME=sales.us.acme.com)));User Id=scott_tiger;Password=test123;"
        ]
    ];

    [Theory]
    [MemberData(nameof(ConnectionStringTestData))]
    public void GetConnectionString_ShouldReturnCorrectlyFormattedConnectionString(
        string? dataSource,
        string? userId,
        string? password,
        string? connectionStringAugment,
        string expected
    )
    {
        var connection = CreateConnection();

        connection.DatabaseName = dataSource;
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
