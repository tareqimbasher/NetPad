using NetPad.Apps.Data.EntityFrameworkCore;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.MariaDb;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.MsSqlServer;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.MySql;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.Oracle;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.PostgreSql;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.SQLite;
using NetPad.DotNet;

namespace NetPad.Apps.Common.Tests.Data.EntityFrameworkCore;

public class EntityFrameworkPackageUtilsTests
{
    private static readonly DotNetFrameworkVersion[] _frameworks =
    [
        DotNetFrameworkVersion.DotNet6,
        DotNetFrameworkVersion.DotNet7,
        DotNetFrameworkVersion.DotNet8,
        DotNetFrameworkVersion.DotNet9,
        DotNetFrameworkVersion.DotNet10,
    ];

    private static readonly Func<EntityFrameworkDatabaseConnection>[] _connections =
    [
        () => new MsSqlServerDatabaseConnection(Guid.NewGuid(), "test"),
        () => new SQLiteDatabaseConnection(Guid.NewGuid(), "test"),
        () => new PostgreSqlDatabaseConnection(Guid.NewGuid(), "test"),
        () => new MySqlDatabaseConnection(Guid.NewGuid(), "test"),
        () => new MariaDbDatabaseConnection(Guid.NewGuid(), "test"),
        () => new OracleDatabaseConnection(Guid.NewGuid(), "test"),
    ];

    public static TheoryData<int, DotNetFrameworkVersion> Combinations()
    {
        var data = new TheoryData<int, DotNetFrameworkVersion>();

        for (var i = 0; i < _connections.Length; i++)
        {
            foreach (var framework in _frameworks)
            {
                data.Add(i, framework);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Combinations))]
    public void ProviderReference_IsTheConnectionsProviderAtAStableVersion(
        int connectionIndex,
        DotNetFrameworkVersion framework)
    {
        var connection = _connections[connectionIndex]();

        var provider = EntityFrameworkPackageUtils.GetProviderReference(connection, framework);

        Assert.Equal(connection.EntityFrameworkProviderName, provider.PackageId);
        Assert.False(new SemanticVersion(provider.Version).IsPrerelease, provider.Version);
    }

    [Theory]
    [InlineData(DotNetFrameworkVersion.DotNet5)]
    [InlineData((DotNetFrameworkVersion)int.MaxValue)]
    public void ProviderReference_ThrowsForUnsupportedFrameworkVersion(DotNetFrameworkVersion framework)
    {
        var connection = new SQLiteDatabaseConnection(Guid.NewGuid(), "test");

        Assert.Throws<ArgumentOutOfRangeException>(
            () => EntityFrameworkPackageUtils.GetProviderReference(connection, framework));
    }

    [Fact]
    public void DesignTimeReferences_AreDesignAndToolsAtTheGivenVersion()
    {
        var packages = EntityFrameworkPackageUtils.GetDesignTimeReferences("9.0.4");

        Assert.Equal(
            [
                (EntityFrameworkPackageUtils.DesignPackageId, "9.0.4"),
                (EntityFrameworkPackageUtils.ToolsPackageId, "9.0.4")
            ],
            packages.Select(p => (p.PackageId, p.Version)));
    }
}
