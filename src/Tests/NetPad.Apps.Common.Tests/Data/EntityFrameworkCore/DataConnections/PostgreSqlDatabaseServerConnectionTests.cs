using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.PostgreSql;
using NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;

namespace NetPad.Apps.Common.Tests.Data.EntityFrameworkCore.DataConnections;

public class PostgreSqlDatabaseServerConnectionTests
{
    [Fact]
    public void DatabaseConnectionUsesServerProperties()
    {
        var serverConnection = new PostgreSqlDatabaseServerConnection(Guid.NewGuid(), "test")
        {
            Host = "foo1",
            Port = "foo2",
            UserId = "foo3",
            Password = "foo4",
            ContainsProductionData = true,
            ConnectionStringAugment =  "foo5"
        };

        var dbConnection = new PostgreSqlDatabaseConnection(Guid.NewGuid(), "test", new ScaffoldOptions());
        dbConnection.SetServer(serverConnection);

        Assert.Equal("foo1", dbConnection.Host);
        Assert.Equal("foo2", dbConnection.Port);
        Assert.Equal("foo3", dbConnection.UserId);
        Assert.Equal("foo4", dbConnection.Password);
        Assert.True(dbConnection.ContainsProductionData);
        Assert.Equal("foo5", dbConnection.ConnectionStringAugment);
    }

    [Fact]
    public void CannotSetDatabaseConnectionPropertiesWhenAttachedToServer()
    {
        var serverConnection = new PostgreSqlDatabaseServerConnection(Guid.NewGuid(), "test")
        {
            Host = "foo1",
            Port = "foo2",
            UserId = "foo3",
            Password = "foo4",
        };

        var dbConnection = new PostgreSqlDatabaseConnection(Guid.NewGuid(), "test", new ScaffoldOptions());
        dbConnection.SetServer(serverConnection);

        Assert.Throws<InvalidOperationException>(() => dbConnection.Host = "bar");
        Assert.Throws<InvalidOperationException>(() => dbConnection.Port = "bar");
        Assert.Throws<InvalidOperationException>(() => dbConnection.UserId = "bar");
        Assert.Throws<InvalidOperationException>(() => dbConnection.Password = "bar");
        Assert.Throws<InvalidOperationException>(() => dbConnection.ContainsProductionData = true);
        Assert.Throws<InvalidOperationException>(() => dbConnection.ConnectionStringAugment = "bar");
    }
}
