using System;
using NetPad.DotNet;

namespace NetPad.Common;

/// <summary>
/// Global constants.
/// </summary>
public static class GlobalConsts
{
    /// <summary>
    /// The .NET Runtime version the app is currently running on.
    /// </summary>
    public static DotNetFrameworkVersion AppDotNetFrameworkVersion { get; }
        = DotNetFrameworkVersionUtil.GetDotNetFrameworkVersion(Environment.Version.Major);

    /// <summary>
    /// If a data connection resource cache was created with an app version before this version, it will be invalidated.
    /// </summary>
    public static SemanticVersion DataConnectionCacheValidOnOrAfterAppVersion { get; } = new(0, 7, 0);

    /// <summary>
    /// Retrieves the version of the Entity Framework driver library based on the specified .NET framework version and provider name.
    /// </summary>
    /// <param name="dotNetFrameworkVersion">The .NET framework version.</param>
    /// <param name="providerName">The name of the provider.</param>
    /// <returns>The version of the Entity Framework driver library corresponding to the specified .NET framework version and provider name.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the specified framework version is not supported.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the version for the specified provider and .NET version could not be determined.</exception>
    public static string EntityFrameworkLibVersion(DotNetFrameworkVersion dotNetFrameworkVersion, string providerName)
    {
        if (providerName is "Microsoft.EntityFrameworkCore.SqlServer" or "Microsoft.EntityFrameworkCore.Design")
        {
            return dotNetFrameworkVersion switch
            {
                DotNetFrameworkVersion.DotNet6 => "6.0.19",
                DotNetFrameworkVersion.DotNet7 => "7.0.8",
                DotNetFrameworkVersion.DotNet8 => "8.0.5",
                DotNetFrameworkVersion.DotNet9 => "9.0.0-preview.3.24172.4",
                _ => throw new ArgumentOutOfRangeException(nameof(dotNetFrameworkVersion), dotNetFrameworkVersion,
                    $"Unsupported framework version: {dotNetFrameworkVersion}")
            };
        }

        if (providerName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            return dotNetFrameworkVersion switch
            {
                DotNetFrameworkVersion.DotNet6 => "6.0.8",
                DotNetFrameworkVersion.DotNet7 => "7.0.4",
                DotNetFrameworkVersion.DotNet8 => "8.0.4",
                DotNetFrameworkVersion.DotNet9 => "9.0.0-preview.3",
                _ => throw new ArgumentOutOfRangeException(nameof(dotNetFrameworkVersion), dotNetFrameworkVersion,
                    $"Unsupported framework version: {dotNetFrameworkVersion}")
            };
        }

        if (providerName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            return dotNetFrameworkVersion switch
            {
                DotNetFrameworkVersion.DotNet6 => "6.0.21",
                DotNetFrameworkVersion.DotNet7 => "7.0.10",
                DotNetFrameworkVersion.DotNet8 => "8.0.5",
                DotNetFrameworkVersion.DotNet9 => "9.0.0-preview.3.24172.4",
                _ => throw new ArgumentOutOfRangeException(nameof(dotNetFrameworkVersion), dotNetFrameworkVersion,
                    $"Unsupported framework version: {dotNetFrameworkVersion}")
            };
        }

        throw new InvalidOperationException($"Could not determine version for provider: '{providerName}' and .NET version: '{dotNetFrameworkVersion}'");
    }
}
