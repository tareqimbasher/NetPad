using NetPad.Apps.Data.DataConnectionFiles;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.PostgreSql;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.SQLite;
using NetPad.Common;
using NetPad.Data;

namespace NetPad.Apps.Common.Tests.Data.DataConnectionFiles;

public class DataConnectionFileMigrationTests
{
    [Fact]
    public void CanDeserializeV0()
    {
        var connections = JsonSerializer.Deserialize<DataConnectionFileV0>(V0Json);
        Assert.NotNull(connections);

        var first = (SQLiteDatabaseConnection)connections.First().Value;
        var second = (PostgreSqlDatabaseConnection)connections.Last().Value;

        Assert.Equal(2, connections.Count);

        Assert.Equal(Guid.Parse("5f368e76-0d41-407b-8594-22087b09c41d"), first.Id);
        Assert.Equal("Connection 1", first.Name);
        Assert.Equal(DataConnectionType.SQLite, first.Type);
        Assert.NotNull(first.ScaffoldOptions);
        Assert.False(first.ScaffoldOptions.NoPluralize);
        Assert.Empty(first.ScaffoldOptions.Tables);
        Assert.True(first.ScaffoldOptions.OptimizeDbContext);

        Assert.Equal(Guid.Parse("2fedcb02-7060-4833-b3fc-b8ba207fcdb2"), second.Id);
        Assert.Equal("Connection 2", second.Name);
        Assert.Equal(DataConnectionType.PostgreSQL, second.Type);
        Assert.NotNull(second.ScaffoldOptions);
        Assert.True(second.ScaffoldOptions.NoPluralize);
        Assert.False(second.ScaffoldOptions.OptimizeDbContext);
        Assert.Equal(["Table 1", "Table 2"], second.ScaffoldOptions.Tables);
    }

    [Fact]
    public void CanMigrateV0ToV1()
    {
        var fileMigrationPipeline = new JsonMigrationPipeline([new DataConnectionFileV0ToV1MigrationStep()]);
        var v1File = fileMigrationPipeline.MigrateToLatest<DataConnectionFileV1>(V0Json, JsonSerializer.DefaultOptions);

        var connections = v1File.Connections;
        Assert.NotNull(connections);

        var first = (SQLiteDatabaseConnection)connections.First();
        var second = (PostgreSqlDatabaseConnection)connections.Last();

        Assert.Equal(2, connections.Count);

        Assert.Equal(Guid.Parse("5f368e76-0d41-407b-8594-22087b09c41d"), first.Id);
        Assert.Equal("Connection 1", first.Name);
        Assert.Equal(DataConnectionType.SQLite, first.Type);
        Assert.NotNull(first.ScaffoldOptions);
        Assert.False(first.ScaffoldOptions.NoPluralize);
        Assert.Empty(first.ScaffoldOptions.Tables);
        Assert.True(first.ScaffoldOptions.OptimizeDbContext);

        Assert.Equal(Guid.Parse("2fedcb02-7060-4833-b3fc-b8ba207fcdb2"), second.Id);
        Assert.Equal("Connection 2", second.Name);
        Assert.Equal(DataConnectionType.PostgreSQL, second.Type);
        Assert.NotNull(second.ScaffoldOptions);
        Assert.True(second.ScaffoldOptions.NoPluralize);
        Assert.False(second.ScaffoldOptions.OptimizeDbContext);
        Assert.Equal(["Table 1", "Table 2"], second.ScaffoldOptions.Tables);

    }

    private const string V0Json =
        """
        {
            "5f368e76-0d41-407b-8594-22087b09c41d": {
              "discriminator": "SQLiteDatabaseConnection",
              "entityFrameworkProviderName": "Microsoft.EntityFrameworkCore.Sqlite",
              "scaffoldOptions": {
                "noPluralize": false,
                "useDatabaseNames": false,
                "schemas": [],
                "tables": [],
                "optimizeDbContext": true
              },
              "host": null,
              "port": null,
              "databaseName": "/path/to/chinook.db",
              "userId": null,
              "password": null,
              "containsProductionData": false,
              "connectionStringAugment": null,
              "id": "5f368e76-0d41-407b-8594-22087b09c41d",
              "name": "Connection 1",
              "type": "SQLite"
            },
            "2fedcb02-7060-4833-b3fc-b8ba207fcdb2": {
              "discriminator": "PostgreSqlDatabaseConnection",
              "entityFrameworkProviderName": "Npgsql.EntityFrameworkCore.PostgreSQL",
              "scaffoldOptions": {
                "noPluralize": true,
                "useDatabaseNames": false,
                "schemas": [],
                "tables": ["Table 1", "Table 2"],
                "optimizeDbContext": false
              },
              "host": "localhost",
              "port": "5432",
              "databaseName": "netpad",
              "userId": "postgres",
              "password": "CfDJ8H2jocC7es9KlPbrH4YXFSgFcKzNoVXty6-B_19EGc-5iH9cSaOUijFGoHB3Q-526OPKa8HL8HpqeZYT4sJjzgeoQ16zf6Dg1IBc-qMP7Jan2vsMyPKLILrhz-MC9D9fsw",
              "containsProductionData": false,
              "connectionStringAugment": null,
              "id": "2fedcb02-7060-4833-b3fc-b8ba207fcdb2",
              "name": "Connection 2",
              "type": "PostgreSQL"
            }
        }
        """;
}
