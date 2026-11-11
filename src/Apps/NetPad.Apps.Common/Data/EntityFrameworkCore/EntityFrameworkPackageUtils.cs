using NetPad.Apps.Data.EntityFrameworkCore.DataConnections;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.MsSqlServer;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.MySql;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.Oracle;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.PostgreSql;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.SQLite;
using NetPad.DotNet;
using NetPad.DotNet.References;

namespace NetPad.Apps.Data.EntityFrameworkCore;

/// <summary>
/// Chooses the EntityFramework Core packages that data connection scripts and scaffold projects reference.
/// </summary>
/// <remarks>
/// Only the provider package is pinned per .NET version. The design-time packages must match the EF Core version that
/// the provider resolves to, otherwise they can pull EF Core above what the script's own reference to the provider
/// resolves to, and the script fails to compile against the scaffolded assembly (CS1705).
/// </remarks>
public static class EntityFrameworkPackageUtils
{
    /// <summary>
    /// The package of EF Core itself, which every provider depends on.
    /// </summary>
    public const string EntityFrameworkCorePackageId = "Microsoft.EntityFrameworkCore";

    /// <summary>
    /// The package that contains the design-time services EF Core's tool uses to scaffold.
    /// </summary>
    public const string DesignPackageId = "Microsoft.EntityFrameworkCore.Design";

    /// <summary>
    /// The package that contains EF Core's design-time tool, <c>ef.dll</c>.
    /// </summary>
    public const string ToolsPackageId = "Microsoft.EntityFrameworkCore.Tools";

    /// <summary>
    /// Gets the package of the EF Core provider a connection uses, at the version to use for a .NET framework version.
    /// This is the only EF Core package a script that uses the connection references.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="dotNetFrameworkVersion">The target .NET framework version.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the connection's provider or the .NET framework version is not supported.
    /// </exception>
    public static PackageReference GetProviderReference(
        EntityFrameworkDatabaseConnection connection,
        DotNetFrameworkVersion dotNetFrameworkVersion)
    {
        var providerName = connection.EntityFrameworkProviderName;
        var version = GetProviderVersion(providerName, dotNetFrameworkVersion);

        return new PackageReference(providerName, providerName, version);
    }

    /// <summary>
    /// Gets the packages a scaffold project needs in order to scaffold at design time:
    /// <c>Microsoft.EntityFrameworkCore.Design</c> and <c>Microsoft.EntityFrameworkCore.Tools</c>.
    /// </summary>
    /// <param name="entityFrameworkCoreVersion">
    /// The version of EF Core that a restore resolved for the provider package. Both packages are pinned to it.
    /// </param>
    public static PackageReference[] GetDesignTimeReferences(string entityFrameworkCoreVersion)
    {
        return
        [
            new PackageReference(DesignPackageId, DesignPackageId, entityFrameworkCoreVersion),
            new PackageReference(ToolsPackageId, ToolsPackageId, entityFrameworkCoreVersion),
        ];
    }

    // The newest stable provider release for each .NET version. Where the provider has none for a .NET version yet,
    // it falls back to the newest release for an older one.
    private static string GetProviderVersion(string providerName, DotNetFrameworkVersion dotNetFrameworkVersion)
    {
        return providerName switch
        {
            MsSqlServerDatabaseConnection.ProviderName or SQLiteDatabaseConnection.ProviderName =>
                dotNetFrameworkVersion switch
                {
                    DotNetFrameworkVersion.DotNet6  => "6.0.36",
                    DotNetFrameworkVersion.DotNet7  => "7.0.20",
                    DotNetFrameworkVersion.DotNet8  => "8.0.34",
                    DotNetFrameworkVersion.DotNet9  => "9.0.23",
                    DotNetFrameworkVersion.DotNet10 => "10.0.15",
                    DotNetFrameworkVersion.DotNet11 => "11.0.0",
                    _                               => throw UnsupportedFrameworkVersion(dotNetFrameworkVersion)
                },
            PostgreSqlDatabaseConnection.ProviderName => dotNetFrameworkVersion switch
            {
                DotNetFrameworkVersion.DotNet6  => "6.0.29",
                DotNetFrameworkVersion.DotNet7  => "7.0.18",
                DotNetFrameworkVersion.DotNet8  => "8.0.11",
                DotNetFrameworkVersion.DotNet9  => "9.0.4",
                DotNetFrameworkVersion.DotNet10 => "10.0.3",
                DotNetFrameworkVersion.DotNet11 => "10.0.3",
                _                               => throw UnsupportedFrameworkVersion(dotNetFrameworkVersion)
            },
            MySqlDatabaseConnection.ProviderName => dotNetFrameworkVersion switch
            {
                DotNetFrameworkVersion.DotNet6  => "6.0.3",
                DotNetFrameworkVersion.DotNet7  => "7.0.0",
                DotNetFrameworkVersion.DotNet8  => "8.0.3",
                DotNetFrameworkVersion.DotNet9  => "9.0.0",
                DotNetFrameworkVersion.DotNet10 => "9.0.0",
                DotNetFrameworkVersion.DotNet11 => "9.0.0",
                _                               => throw UnsupportedFrameworkVersion(dotNetFrameworkVersion)
            },
            OracleDatabaseConnection.ProviderName => dotNetFrameworkVersion switch
            {
                DotNetFrameworkVersion.DotNet6  => "6.21.170",
                DotNetFrameworkVersion.DotNet7  => "7.21.13",
                DotNetFrameworkVersion.DotNet8  => "8.23.26400",
                DotNetFrameworkVersion.DotNet9  => "9.23.26400",
                DotNetFrameworkVersion.DotNet10 => "10.23.26400",
                DotNetFrameworkVersion.DotNet11 => "10.23.26400",
                _                               => throw UnsupportedFrameworkVersion(dotNetFrameworkVersion)
            },
            _ => throw new ArgumentOutOfRangeException(nameof(providerName), providerName, "Unsupported provider")
        };
    }

    private static ArgumentOutOfRangeException UnsupportedFrameworkVersion(
        DotNetFrameworkVersion dotNetFrameworkVersion)
    {
        return new ArgumentOutOfRangeException(
            nameof(dotNetFrameworkVersion),
            dotNetFrameworkVersion,
            "Unsupported framework version");
    }
}
