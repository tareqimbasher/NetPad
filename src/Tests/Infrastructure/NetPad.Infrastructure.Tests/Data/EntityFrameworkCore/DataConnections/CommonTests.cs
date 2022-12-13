using NetPad.Data;
using NetPad.Data.EntityFrameworkCore.DataConnections;
using Xunit;

namespace NetPad.Infrastructure.Tests.Data.EntityFrameworkCore.DataConnections;

public abstract class CommonTests
{
    protected CommonTests(DataConnectionType type, string entityFrameworkProviderName)
    {
        Type = type;
        EntityFrameworkProviderName = entityFrameworkProviderName;
    }

    public DataConnectionType Type { get; }
    public string EntityFrameworkProviderName { get; }

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
