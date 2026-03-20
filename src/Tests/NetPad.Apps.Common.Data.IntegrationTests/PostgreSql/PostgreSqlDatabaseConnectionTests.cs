using Microsoft.EntityFrameworkCore;
using NetPad.Apps.Data.EntityFrameworkCore;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.PostgreSql;
using NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;
using Testcontainers.PostgreSql;

namespace NetPad.Apps.Common.Data.IntegrationTests.PostgreSql;

public sealed class PostgreSqlDatabaseConnectionTests : IAsyncLifetime
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

    private PostgreSqlDatabaseConnection CreateConnection(string? databaseName = null) =>
        new(Guid.NewGuid(), "test", new ScaffoldOptions())
        {
            Host = _postgres.Hostname,
            Port = _postgres.GetMappedPublicPort().ToString(),
            UserId = "postgres",
            Password = "postgres",
            DatabaseName = databaseName
        };

    [Fact]
    public async Task TestConnectionSucceedsWithNoDatabaseSpecified()
    {
        var dbConnection = CreateConnection();

        var result = await dbConnection.TestConnectionAsync(new NoOpDataConnectionPasswordProtector());

        Assert.True(result.Success, result.Message);
    }

    [Fact]
    public async Task TestConnectionSucceedsWithDatabaseSpecified()
    {
        await SeedDatabaseAsync();
        var dbConnection = CreateConnection("LibraryDb");

        var result = await dbConnection.TestConnectionAsync(new NoOpDataConnectionPasswordProtector());

        Assert.True(result.Success, result.Message);
    }

    [Fact]
    public async Task TestConnectionFailsIfDatabaseDoesNotExist()
    {
        var dbConnection = CreateConnection("DoesNotExist");

        var result = await dbConnection.TestConnectionAsync(new NoOpDataConnectionPasswordProtector());

        Assert.False(result.Success);
    }

    [Fact]
    public async Task CanListDatabases()
    {
        await SeedDatabaseAsync();
        var dbConnection = CreateConnection();

        var databases = await dbConnection.GetDatabasesAsync(new NoOpDataConnectionPasswordProtector());

        Assert.Contains("LibraryDb", databases);
    }

    [Fact]
    public async Task CanDoBasicQuery()
    {
        await SeedDatabaseAsync();
        var dbConnection = CreateConnection();
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
