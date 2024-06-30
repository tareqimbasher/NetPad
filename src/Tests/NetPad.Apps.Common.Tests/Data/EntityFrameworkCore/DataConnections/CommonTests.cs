using NetPad.Apps.Data.EntityFrameworkCore.DataConnections;
using NetPad.Data;

namespace NetPad.Apps.Common.Tests.Data.EntityFrameworkCore.DataConnections;

public abstract class CommonTests(DataConnectionType type, string entityFrameworkProviderName)
{
    public DataConnectionType Type { get; } = type;
    public string EntityFrameworkProviderName { get; } = entityFrameworkProviderName;

    protected abstract EntityFrameworkDatabaseConnection CreateConnection();

    [Fact]
    public void ConnectionType_ShouldBeCorrect()
    {
        var connection = CreateConnection();

        Assert.Equal(Type, connection.Type);
    }

    [Fact]
    public void EntityFrameworkProviderName_ShouldBeCorrect()
    {
        var connection = CreateConnection();

        Assert.Equal(EntityFrameworkProviderName, connection.EntityFrameworkProviderName);
    }
}
