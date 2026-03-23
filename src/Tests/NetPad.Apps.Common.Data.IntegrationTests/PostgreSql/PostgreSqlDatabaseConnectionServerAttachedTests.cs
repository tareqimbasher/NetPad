using Microsoft.EntityFrameworkCore;
using NetPad.Apps.Data.EntityFrameworkCore;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.PostgreSql;
using NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;
using Testcontainers.PostgreSql;

namespace NetPad.Apps.Common.Data.IntegrationTests.PostgreSql;

public sealed class PostgreSqlDatabaseConnectionServerAttachedTests : IAsyncLifetime
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
        var dbConnection = new PostgreSqlDatabaseConnection(Guid.NewGuid(), "test", new ScaffoldOptions());
        dbConnection.SetServer(serverConnection);

        var result = await dbConnection.TestConnectionAsync(new NoOpDataConnectionPasswordProtector());

        Assert.True(result.Success, result.Message);
    }

    [Fact]
    public async Task CanDoBasicQuery()
    {
        await SeedDatabaseAsync();
        var serverConnection = CreateServerConnection();
        var dbConnection = new PostgreSqlDatabaseConnection(Guid.NewGuid(), "test", new ScaffoldOptions());
        dbConnection.SetServer(serverConnection);
        var dbContext = dbConnection.CreateDbContext(new NoOpDataConnectionPasswordProtector());
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT \"Name\" FROM \"Author\" ORDER BY \"Id\"";
        await dbContext.Database.OpenConnectionAsync();

        await using var result = await command.ExecuteReaderAsync();
        var authors = new List<string>();
        while (await result.ReadAsync())
        {
            authors.Add((string)result["Name"]);
        }

        Assert.Equal(["Jane Austen", "George Orwell", "Mary Shelley"], authors);
    }
}
