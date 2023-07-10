using System;
using NetPad.DotNet;

namespace NetPad.Common;

/// <summary>
/// Global constants.
/// </summary>
public static class GlobalConsts
{
    public static DotNetFrameworkVersion AppDotNetFrameworkVersion { get; }
        = DotNetFrameworkVersionUtil.GetDotNetFrameworkVersion(Environment.Version.Major);

    public static string EntityFrameworkLibVersion(DotNetFrameworkVersion dotNetFrameworkVersion, string providerName)
    {
        if (providerName is "Microsoft.EntityFrameworkCore.SqlServer" or "Microsoft.EntityFrameworkCore.Design")
        {
            return dotNetFrameworkVersion switch
            {
                DotNetFrameworkVersion.DotNet6 => "6.0.19",
                DotNetFrameworkVersion.DotNet7 => "7.0.8",
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
                _ => throw new ArgumentOutOfRangeException(nameof(dotNetFrameworkVersion), dotNetFrameworkVersion,
                    $"Unsupported framework version: {dotNetFrameworkVersion}")
            };
        }

        throw new InvalidOperationException($"Could not determine version for provider: '{providerName}' and .NET version: '{dotNetFrameworkVersion}'");
    }
}
