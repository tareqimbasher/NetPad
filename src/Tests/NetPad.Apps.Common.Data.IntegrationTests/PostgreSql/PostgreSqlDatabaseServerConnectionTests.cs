using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.PostgreSql;
using Testcontainers.PostgreSql;

namespace NetPad.Apps.Common.Data.IntegrationTests.PostgreSql;

public class PostgreSqlDatabaseServerConnectionTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine").Build();

    public Task InitializeAsync()
    {
        return _postgres.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _postgres.DisposeAsync().AsTask();
    }

    private Task SeedDatabaseAsync()
    {
        return _postgres.ExecScriptAsync(File.ReadAllText("PostgreSql/postgres-seed.sql"));
    }

    private PostgreSqlDatabaseServerConnection CreateServerConnection() =>
        new(Guid.NewGuid(), "test")
        {
            Host = _postgres.Hostname,
            Port = _postgres.GetMappedPublicPort().ToString(),
            UserId = "postgres",
            Password = "postgres",
        };

    [Fact]
    public async Task TestConnectionSucceeds()
    {
        var serverConnection = CreateServerConnection();

        var result = await serverConnection.TestConnectionAsync(new NoOpDataConnectionPasswordProtector());

        Assert.True(result.Success, result.Message);
    }

    [Fact]
    public async Task CanListDatabases()
    {
        await SeedDatabaseAsync();
        var serverConnection = CreateServerConnection();

        var databases = await serverConnection.GetDatabasesAsync(new NoOpDataConnectionPasswordProtector());

        Assert.Contains("LibraryDb", databases);
    }
}
