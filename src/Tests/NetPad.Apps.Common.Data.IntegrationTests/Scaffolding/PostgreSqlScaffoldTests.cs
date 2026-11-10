using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.PostgreSql;
using NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;
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
        await CreatePersonTableAsync();

        var code = await ScaffolderTestHelper.ScaffoldEntityCodeAsync(CreateConnection(), framework, "Person.cs");

        Assert.Contains("public string? Nickname { get; set; }", code);
        Assert.Contains("public string Name { get; set; } = null!;", code);
    }

    [Theory]
    [MemberData(nameof(Frameworks))]
    public async Task ScaffoldConnectionResources_WithOptimizeDbContext_BuildsCompiledModel(DotNetFrameworkVersion framework)
    {
        await CreatePersonTableAsync();

        var connection = CreateConnection(new ScaffoldOptions { OptimizeDbContext = true });

        await ScaffolderTestHelper.AssertScaffoldsWithCompiledModelAsync(connection, framework);
    }

    private Task CreatePersonTableAsync() => _postgres.ExecScriptAsync(
        "CREATE TABLE person (id SERIAL PRIMARY KEY, name TEXT NOT NULL, nickname TEXT NULL);");

    private PostgreSqlDatabaseConnection CreateConnection(ScaffoldOptions? scaffoldOptions = null) =>
        new(Guid.NewGuid(), "test", scaffoldOptions)
        {
            Host = _postgres.Hostname,
            Port = _postgres.GetMappedPublicPort().ToString(),
            UserId = "postgres",
            Password = "postgres",
            DatabaseName = "postgres"
        };
}
