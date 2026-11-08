using NetPad.Apps.Data.EntityFrameworkCore.DataConnections;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.MsSqlServer;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.MySql;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.Oracle;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.PostgreSql;
using NetPad.Apps.Data.EntityFrameworkCore.DataConnections.SQLite;
using NetPad.DotNet;
using NetPad.DotNet.References;

namespace NetPad.Apps.Data.EntityFrameworkCore;

public static class EntityFrameworkPackageUtils
{
    private const string DesignPackageId = "Microsoft.EntityFrameworkCore.Design";

    /// <summary>
    /// The package that contains EF Core's design-time tool, <c>ef.dll</c>.
    /// </summary>
    public const string ToolsPackageId = "Microsoft.EntityFrameworkCore.Tools";

    /// <summary>
    /// Gets the required EntityFramework packages for a connection for a specified .NET framework version.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="dotNetFrameworkVersion">The target .NET framework version.</param>
    /// <param name="includeDesignTimePackages">
    /// Whether to include the packages needed to scaffold at design time: Microsoft.EntityFrameworkCore.Design and
    /// Microsoft.EntityFrameworkCore.Tools. Script projects should never get these.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified .NET framework version is not supported.</exception>
    public static Reference[] GetRequiredReferences(
        EntityFrameworkDatabaseConnection connection,
        DotNetFrameworkVersion dotNetFrameworkVersion,
        bool includeDesignTimePackages = false)
    {
        string providerName = connection.EntityFrameworkProviderName;
        var packages = new List<PackageReference>();

        void AddDesignTimePackages(string version)
        {
            packages.Add(new PackageReference(DesignPackageId, DesignPackageId, version));
            packages.Add(new PackageReference(ToolsPackageId, ToolsPackageId, version));
        }

        if (providerName == MsSqlServerDatabaseConnection.ProviderName)
        {
            var version = dotNetFrameworkVersion switch
            {
                DotNetFrameworkVersion.DotNet6 => "6.0.33",
                DotNetFrameworkVersion.DotNet7 => "7.0.20",
                DotNetFrameworkVersion.DotNet8 => "8.0.8",
                DotNetFrameworkVersion.DotNet9 => "9.0.4",
                DotNetFrameworkVersion.DotNet10 => "10.0.0",
                _ => throw new ArgumentOutOfRangeException(nameof(dotNetFrameworkVersion), dotNetFrameworkVersion, "Unsupported framework version")
            };

            packages.Add(new PackageReference(providerName, providerName, version));

            if (includeDesignTimePackages)
            {
                version = dotNetFrameworkVersion switch
                {
                    DotNetFrameworkVersion.DotNet6 => "6.0.33",
                    DotNetFrameworkVersion.DotNet7 => "7.0.20",
                    DotNetFrameworkVersion.DotNet8 => "8.0.8",
                    DotNetFrameworkVersion.DotNet9 => "9.0.4",
                    DotNetFrameworkVersion.DotNet10 => "10.0.0",
                    _ => throw new ArgumentOutOfRangeException(nameof(dotNetFrameworkVersion), dotNetFrameworkVersion, "Unsupported framework version")
                };

                AddDesignTimePackages(version);
            }
        }
        else if (providerName == SQLiteDatabaseConnection.ProviderName)
        {
            var version = dotNetFrameworkVersion switch
            {
                DotNetFrameworkVersion.DotNet6 => "6.0.33",
                DotNetFrameworkVersion.DotNet7 => "7.0.20",
                DotNetFrameworkVersion.DotNet8 => "8.0.8",
                DotNetFrameworkVersion.DotNet9 => "9.0.4",
                DotNetFrameworkVersion.DotNet10 => "10.0.0",
                _ => throw new ArgumentOutOfRangeException(nameof(dotNetFrameworkVersion), dotNetFrameworkVersion, "Unsupported framework version")
            };

            packages.Add(new PackageReference(providerName, providerName, version));

            if (includeDesignTimePackages)
            {
                version = dotNetFrameworkVersion switch
                {
                    DotNetFrameworkVersion.DotNet6 => "6.0.33",
                    DotNetFrameworkVersion.DotNet7 => "7.0.20",
                    DotNetFrameworkVersion.DotNet8 => "8.0.5",
                    DotNetFrameworkVersion.DotNet9 => "9.0.4",
                    DotNetFrameworkVersion.DotNet10 => "10.0.0",
                    _ => throw new ArgumentOutOfRangeException(nameof(dotNetFrameworkVersion), dotNetFrameworkVersion, "Unsupported framework version")
                };

                AddDesignTimePackages(version);
            }
        }
        else if (providerName == PostgreSqlDatabaseConnection.ProviderName)
        {
            var version = dotNetFrameworkVersion switch
            {
                DotNetFrameworkVersion.DotNet6 => "6.0.29",
                DotNetFrameworkVersion.DotNet7 => "7.0.18",
                DotNetFrameworkVersion.DotNet8 => "8.0.4",
                DotNetFrameworkVersion.DotNet9 => "9.0.4",
                DotNetFrameworkVersion.DotNet10 => "10.0.0-rc.2",
                _ => throw new ArgumentOutOfRangeException(nameof(dotNetFrameworkVersion), dotNetFrameworkVersion, "Unsupported framework version")
            };

            packages.Add(new PackageReference(providerName, providerName, version));

            if (includeDesignTimePackages)
            {
                version = dotNetFrameworkVersion switch
                {
                    DotNetFrameworkVersion.DotNet6 => "6.0.29",
                    DotNetFrameworkVersion.DotNet7 => "7.0.18",
                    DotNetFrameworkVersion.DotNet8 => "8.0.4",
                    DotNetFrameworkVersion.DotNet9 => "9.0.1",
                    DotNetFrameworkVersion.DotNet10 => "10.0.0-rc.2.25502.107",
                    _ => throw new ArgumentOutOfRangeException(nameof(dotNetFrameworkVersion), dotNetFrameworkVersion, "Unsupported framework version")
                };

                AddDesignTimePackages(version);
            }
        }
        else if (providerName == MySqlDatabaseConnection.ProviderName)
        {
            var version = dotNetFrameworkVersion switch
            {
                DotNetFrameworkVersion.DotNet6 => "6.0.3",
                DotNetFrameworkVersion.DotNet7 => "7.0.0",
                DotNetFrameworkVersion.DotNet8 => "8.0.2",
                DotNetFrameworkVersion.DotNet9 => "9.0.0",
                DotNetFrameworkVersion.DotNet10 => "9.0.0", // There is no v10 yet
                _ => throw new ArgumentOutOfRangeException(nameof(dotNetFrameworkVersion), dotNetFrameworkVersion, "Unsupported framework version")
            };

            packages.Add(new PackageReference(providerName, providerName, version));

            if (includeDesignTimePackages)
            {
                version = dotNetFrameworkVersion switch
                {
                    DotNetFrameworkVersion.DotNet6 => "6.0.28",
                    DotNetFrameworkVersion.DotNet7 => "7.0.2",
                    DotNetFrameworkVersion.DotNet8 => "8.0.2",
                    DotNetFrameworkVersion.DotNet9 => "9.0.0",
                    DotNetFrameworkVersion.DotNet10 => "9.0.0", // There is no v10 yet
                    _ => throw new ArgumentOutOfRangeException(nameof(dotNetFrameworkVersion), dotNetFrameworkVersion, "Unsupported framework version")
                };

                AddDesignTimePackages(version);
            }
        }
        else if (providerName == OracleDatabaseConnection.ProviderName)
        {
            var version = dotNetFrameworkVersion switch
            {
                DotNetFrameworkVersion.DotNet6 => "6.21.170",
                DotNetFrameworkVersion.DotNet7 => "7.21.13",
                DotNetFrameworkVersion.DotNet8 => "8.23.90",
                DotNetFrameworkVersion.DotNet9 => "9.23.60",
                DotNetFrameworkVersion.DotNet10 => "9.23.60",
                _ => throw new ArgumentOutOfRangeException(nameof(dotNetFrameworkVersion), dotNetFrameworkVersion, "Unsupported framework version")
            };

            packages.Add(new PackageReference(providerName, providerName, version));

            if (includeDesignTimePackages)
            {
                version = dotNetFrameworkVersion switch
                {
                    DotNetFrameworkVersion.DotNet6 => "6.0.36",
                    DotNetFrameworkVersion.DotNet7 => "7.0.20",
                    DotNetFrameworkVersion.DotNet8 => "8.0.8",
                    DotNetFrameworkVersion.DotNet9 => "9.0.0",
                    DotNetFrameworkVersion.DotNet10 => "9.0.0",
                    _ => throw new ArgumentOutOfRangeException(nameof(dotNetFrameworkVersion), dotNetFrameworkVersion, "Unsupported framework version")
                };

                AddDesignTimePackages(version);
            }
        }

        return packages.ToArray();
    }
}
