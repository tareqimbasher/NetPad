using NetPad.Apps.Data.EntityFrameworkCore.DataConnections;
using NetPad.DotNet;
using NetPad.DotNet.References;

namespace NetPad.Apps.Data.EntityFrameworkCore;

public static class EntityFrameworkPackageUtils
{
    /// <summary>
    /// Gets the required EntityFramework packages for a connection for a specified .NET framework version.
    /// </summary>
    /// <param name="connection">The connection.</param>
    /// <param name="dotNetFrameworkVersion">The target .NET framework version.</param>
    /// <param name="includeDesignPackage">Whether to include a reference to Microsoft.EntityFrameworkCore.Design</param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified .NET framework version is not supported.</exception>
    public static Reference[] GetRequiredReferences(
        EntityFrameworkDatabaseConnection connection,
        DotNetFrameworkVersion dotNetFrameworkVersion,
        bool includeDesignPackage = false)
    {
        string providerName = connection.EntityFrameworkProviderName;
        var packages = new List<PackageReference>();

        if (providerName == MsSqlServerDatabaseConnection.ProviderName)
        {
            var version = dotNetFrameworkVersion switch
            {
                DotNetFrameworkVersion.DotNet6 => "6.0.33",
                DotNetFrameworkVersion.DotNet7 => "7.0.20",
                DotNetFrameworkVersion.DotNet8 => "8.0.8",
                DotNetFrameworkVersion.DotNet9 => "9.0.4",
                _ => throw new ArgumentOutOfRangeException(nameof(dotNetFrameworkVersion), dotNetFrameworkVersion, "Unsupported framework version")
            };

            packages.Add(new PackageReference(providerName, providerName, version));

            if (includeDesignPackage)
            {
                version = dotNetFrameworkVersion switch
                {
                    DotNetFrameworkVersion.DotNet6 => "6.0.33",
                    DotNetFrameworkVersion.DotNet7 => "7.0.20",
                    DotNetFrameworkVersion.DotNet8 => "8.0.8",
                    DotNetFrameworkVersion.DotNet9 => "9.0.4",
                    _ => throw new ArgumentOutOfRangeException(nameof(dotNetFrameworkVersion), dotNetFrameworkVersion, "Unsupported framework version")
                };

                packages.Add(new PackageReference("Microsoft.EntityFrameworkCore.Design", "Microsoft.EntityFrameworkCore.Design", version));
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
                _ => throw new ArgumentOutOfRangeException(nameof(dotNetFrameworkVersion), dotNetFrameworkVersion, "Unsupported framework version")
            };

            packages.Add(new PackageReference(providerName, providerName, version));

            if (includeDesignPackage)
            {
                version = dotNetFrameworkVersion switch
                {
                    DotNetFrameworkVersion.DotNet6 => "6.0.33",
                    DotNetFrameworkVersion.DotNet7 => "7.0.20",
                    DotNetFrameworkVersion.DotNet8 => "8.0.5",
                    DotNetFrameworkVersion.DotNet9 => "9.0.4",
                    _ => throw new ArgumentOutOfRangeException(nameof(dotNetFrameworkVersion), dotNetFrameworkVersion, "Unsupported framework version")
                };

                packages.Add(new PackageReference("Microsoft.EntityFrameworkCore.Design", "Microsoft.EntityFrameworkCore.Design", version));
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
                _ => throw new ArgumentOutOfRangeException(nameof(dotNetFrameworkVersion), dotNetFrameworkVersion, "Unsupported framework version")
            };

            packages.Add(new PackageReference(providerName, providerName, version));

            if (includeDesignPackage)
            {
                version = dotNetFrameworkVersion switch
                {
                    DotNetFrameworkVersion.DotNet6 => "6.0.29",
                    DotNetFrameworkVersion.DotNet7 => "7.0.18",
                    DotNetFrameworkVersion.DotNet8 => "8.0.4",
                    DotNetFrameworkVersion.DotNet9 => "9.0.1",
                    _ => throw new ArgumentOutOfRangeException(nameof(dotNetFrameworkVersion), dotNetFrameworkVersion, "Unsupported framework version")
                };

                packages.Add(new PackageReference("Microsoft.EntityFrameworkCore.Design", "Microsoft.EntityFrameworkCore.Design", version));
            }
        }
        else if (providerName == MySqlDatabaseConnection.ProviderName)
        {
            var version = dotNetFrameworkVersion switch
            {
                DotNetFrameworkVersion.DotNet6 => "6.0.3",
                DotNetFrameworkVersion.DotNet7 => "7.0.0",
                DotNetFrameworkVersion.DotNet8 => "8.0.2",
                DotNetFrameworkVersion.DotNet9 => "9.0.0-preview.3.efcore.9.0.0",
                _ => throw new ArgumentOutOfRangeException(nameof(dotNetFrameworkVersion), dotNetFrameworkVersion, "Unsupported framework version")
            };

            packages.Add(new PackageReference(providerName, providerName, version));

            if (includeDesignPackage)
            {
                version = dotNetFrameworkVersion switch
                {
                    DotNetFrameworkVersion.DotNet6 => "6.0.28",
                    DotNetFrameworkVersion.DotNet7 => "7.0.2",
                    DotNetFrameworkVersion.DotNet8 => "8.0.2",
                    DotNetFrameworkVersion.DotNet9 => "9.0.0",
                    _ => throw new ArgumentOutOfRangeException(nameof(dotNetFrameworkVersion), dotNetFrameworkVersion, "Unsupported framework version")
                };

                packages.Add(new PackageReference("Microsoft.EntityFrameworkCore.Design", "Microsoft.EntityFrameworkCore.Design", version));
            }
        }
        else if (providerName == OracleDatabaseConnection.ProviderName)
        {
            var version = dotNetFrameworkVersion switch
            {
                DotNetFrameworkVersion.DotNet6 => "6.21.170",
                DotNetFrameworkVersion.DotNet7 => "7.21.13",
                DotNetFrameworkVersion.DotNet8 => "8.23.90",
                DotNetFrameworkVersion.DotNet9 => "9.23.90",
                _ => throw new ArgumentOutOfRangeException(nameof(dotNetFrameworkVersion), dotNetFrameworkVersion, "Unsupported framework version")
            };

            packages.Add(new PackageReference(providerName, providerName, version));

            if (includeDesignPackage)
            {
                version = dotNetFrameworkVersion switch
                {
                    DotNetFrameworkVersion.DotNet6 => "6.0.36",
                    DotNetFrameworkVersion.DotNet7 => "7.0.20",
                    DotNetFrameworkVersion.DotNet8 => "8.0.19",
                    DotNetFrameworkVersion.DotNet9 => "9.0.8",
                    _ => throw new ArgumentOutOfRangeException(nameof(dotNetFrameworkVersion), dotNetFrameworkVersion, "Unsupported framework version")
                };

                packages.Add(new PackageReference("Microsoft.EntityFrameworkCore.Design", "Microsoft.EntityFrameworkCore.Design", version));
            }
        }

        return packages.ToArray();
    }
}
