using NetPad.Apps.Data.EntityFrameworkCore;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.MariaDb;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.MsSqlServer;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.MySql;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.Oracle;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.PostgreSql;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.SQLite;
using NetPad.DotNet;
using NetPad.DotNet.References;

namespace NetPad.Apps.Common.Tests.Data.EntityFrameworkCore;

public class EntityFrameworkPackageUtilsTests
{
    private const string DesignPackageId = "Microsoft.EntityFrameworkCore.Design";

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
    public void DesignTimePackages_ToolsVersionMatchesDesignVersion(int connectionIndex, DotNetFrameworkVersion framework)
    {
        var packages = EntityFrameworkPackageUtils
            .GetRequiredReferences(_connections[connectionIndex](), framework, includeDesignTimePackages: true)
            .Cast<PackageReference>()
            .ToArray();

        var design = Assert.Single(packages, p => p.PackageId == DesignPackageId);
        var tools = Assert.Single(packages, p => p.PackageId == EntityFrameworkPackageUtils.ToolsPackageId);

        Assert.Equal(design.Version, tools.Version);
    }

    [Theory]
    [MemberData(nameof(Combinations))]
    public void ScriptProjects_NeverGetDesignTimePackages(int connectionIndex, DotNetFrameworkVersion framework)
    {
        var packages = EntityFrameworkPackageUtils
            .GetRequiredReferences(_connections[connectionIndex](), framework)
            .Cast<PackageReference>()
            .ToArray();

        Assert.DoesNotContain(packages, p => p.PackageId == DesignPackageId);
        Assert.DoesNotContain(packages, p => p.PackageId == EntityFrameworkPackageUtils.ToolsPackageId);
    }
}
