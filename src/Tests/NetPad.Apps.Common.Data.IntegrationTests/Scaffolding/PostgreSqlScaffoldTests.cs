using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.PostgreSql;
using NetPad.DotNet;
using Testcontainers.PostgreSql;

namespace NetPad.Apps.Common.Data.IntegrationTests.Scaffolding;

public sealed class PostgreSqlScaffoldTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:18-alpine").Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    public static TheoryData<DotNetFrameworkVersion> Frameworks() =>
        ScaffolderTestHelper.OldestAndNewestInstalledFrameworks();

    [Theory]
    [MemberData(nameof(Frameworks))]
    public async Task ScaffoldToProject_GeneratesNullableColumnAsNullable(DotNetFrameworkVersion framework)
    {
        await _postgres.ExecScriptAsync(
            "CREATE TABLE person (id SERIAL PRIMARY KEY, name TEXT NOT NULL, nickname TEXT NULL);");

        var connection = new PostgreSqlDatabaseConnection(Guid.NewGuid(), "test")
        {
            Host = _postgres.Hostname,
            Port = _postgres.GetMappedPublicPort().ToString(),
            UserId = "postgres",
            Password = "postgres",
            DatabaseName = "postgres"
        };

        var code = await ScaffolderTestHelper.ScaffoldEntityCodeAsync(connection, framework, "Person.cs");

        Assert.Contains("public string? Nickname { get; set; }", code);
        Assert.Contains("public string Name { get; set; } = null!;", code);
    }
}
