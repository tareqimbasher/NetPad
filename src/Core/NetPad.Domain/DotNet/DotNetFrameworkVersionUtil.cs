using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

namespace NetPad.DotNet;

public static class DotNetFrameworkVersionUtil
{
    private static readonly Dictionary<int, DotNetFrameworkVersion> _sdkVersionMap = new()
    {
        { 2, DotNetFrameworkVersion.DotNet2 },
        { 3, DotNetFrameworkVersion.DotNet3 },
        { 5, DotNetFrameworkVersion.DotNet5 },
        { 6, DotNetFrameworkVersion.DotNet6 },
        { 7, DotNetFrameworkVersion.DotNet7 },
        { 8, DotNetFrameworkVersion.DotNet8 },
    };

    private static readonly Dictionary<DotNetFrameworkVersion, int> _sdkVersionMapReverse = _sdkVersionMap
        .ToDictionary(kv => kv.Value, kv => kv.Key);

    public static string GetTargetFrameworkMoniker(this DotNetFrameworkVersion frameworkVersion)
    {
        return frameworkVersion switch
        {
            DotNetFrameworkVersion.DotNet6 => "net6.0",
            DotNetFrameworkVersion.DotNet7 => "net7.0",
            DotNetFrameworkVersion.DotNet8 => "net8.0",
            _ => throw new ArgumentOutOfRangeException(nameof(frameworkVersion), frameworkVersion, $"Unknown framework version: {frameworkVersion}")
        };
    }

    public static bool TryGetDotNetFrameworkVersion(string targetFrameworkMoniker, [NotNullWhen(true)] out DotNetFrameworkVersion? dotNetFrameworkVersion)
    {
        dotNetFrameworkVersion = targetFrameworkMoniker switch
        {
            "net6.0" => DotNetFrameworkVersion.DotNet6,
            "net7.0" => DotNetFrameworkVersion.DotNet7,
            "net8.0" => DotNetFrameworkVersion.DotNet8,
            _ => null
        };

        return dotNetFrameworkVersion != null;
    }

    public static bool IsSdkVersionSupported(SemanticVersion sdkVersion)
    {
        return sdkVersion.Major is >= 6 and <= 8;
    }

    public static bool IsSupported(this DotNetSdkVersion sdkVersion)
    {
        return IsSdkVersionSupported(sdkVersion.Version);
    }

    public static bool IsEfToolVersionSupported(SemanticVersion efToolVersion)
    {
        return efToolVersion.Major > 5;
    }

    public static DotNetFrameworkVersion FrameworkVersion(this DotNetRuntimeVersion runtimeVersion)
    {
        return GetDotNetFrameworkVersion(runtimeVersion.Version.Major);
    }

    public static DotNetFrameworkVersion FrameworkVersion(this DotNetSdkVersion sdkVersion)
    {
        return GetDotNetFrameworkVersion(sdkVersion.Version.Major);
    }

    public static DotNetFrameworkVersion GetDotNetFrameworkVersion(int majorVersion)
    {
        if (_sdkVersionMap.TryGetValue(majorVersion, out var frameworkVersion))
            return frameworkVersion;

        throw new ArgumentOutOfRangeException(nameof(majorVersion), majorVersion, $"Unknown major version: {majorVersion}");
    }

    public static int GetMajorVersion(this DotNetFrameworkVersion frameworkVersion)
    {
        if (!_sdkVersionMapReverse.TryGetValue(frameworkVersion, out int majorVersion))
            throw new ArgumentOutOfRangeException(nameof(frameworkVersion), frameworkVersion, $"Unknown framework version: {frameworkVersion}");

        return majorVersion;
    }

    public static LanguageVersion GetLatestSupportedCSharpLanguageVersion(this DotNetFrameworkVersion dotNetFrameworkVersion)
    {
        return dotNetFrameworkVersion switch
        {
            DotNetFrameworkVersion.DotNet6 => LanguageVersion.CSharp10,
            DotNetFrameworkVersion.DotNet7 => LanguageVersion.CSharp11,
            DotNetFrameworkVersion.DotNet8 => LanguageVersion.CSharp12,
            _ => throw new ArgumentOutOfRangeException(nameof(dotNetFrameworkVersion), dotNetFrameworkVersion, "Unhandled .NET framework version")
        };
    }
}
