using Microsoft.Data.Sqlite;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.SQLite;
using NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;
using NetPad.DotNet;

namespace NetPad.Apps.Common.Data.IntegrationTests.Scaffolding;

public sealed class SqliteScaffoldTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"netpad-scaffold-{Guid.NewGuid()}.db");

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        File.Delete(_databasePath);
    }

    public static TheoryData<DotNetFrameworkVersion> Frameworks() =>
        ScaffolderTestHelper.OldestAndNewestInstalledFrameworks();

    [Theory]
    [MemberData(nameof(Frameworks))]
    public async Task ScaffoldToProject_GeneratesNullableColumnAsNullable(DotNetFrameworkVersion framework)
    {
        await CreatePersonTableAsync();

        var connection = new SQLiteDatabaseConnection(Guid.NewGuid(), "test") { DatabaseName = _databasePath };

        var code = await ScaffolderTestHelper.ScaffoldEntityCodeAsync(connection, framework, "Person.cs");

        Assert.Contains("public string? Nickname { get; set; }", code);
        Assert.Contains("public string Name { get; set; } = null!;", code);
    }

    [Theory]
    [MemberData(nameof(Frameworks))]
    public async Task ScaffoldConnectionResources_ReportsStagesInOrder(DotNetFrameworkVersion framework)
    {
        await CreatePersonTableAsync();

        var connection = new SQLiteDatabaseConnection(Guid.NewGuid(), "test") { DatabaseName = _databasePath };

        await ScaffolderTestHelper.AssertScaffoldsConnectionResourcesAsync(connection, framework);
    }

    [Theory]
    [MemberData(nameof(Frameworks))]
    public async Task ScaffoldConnectionResources_WithOptimizeDbContext_BuildsCompiledModel(DotNetFrameworkVersion framework)
    {
        await CreatePersonTableAsync();

        var connection = new SQLiteDatabaseConnection(Guid.NewGuid(), "test", new ScaffoldOptions { OptimizeDbContext = true })
        {
            DatabaseName = _databasePath
        };

        await ScaffolderTestHelper.AssertScaffoldsWithCompiledModelAsync(connection, framework);
    }

    private async Task CreatePersonTableAsync()
    {
        await using var db = new SqliteConnection($"Data Source={_databasePath}");
        await db.OpenAsync();
        await using var command = db.CreateCommand();
        command.CommandText = "CREATE TABLE Person (Id INTEGER PRIMARY KEY, Name TEXT NOT NULL, Nickname TEXT NULL)";
        await command.ExecuteNonQueryAsync();
    }
}
