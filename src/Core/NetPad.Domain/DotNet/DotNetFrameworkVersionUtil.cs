using System;
using System.Collections.Generic;
using System.Linq;

namespace NetPad.DotNet;

public static class DotNetFrameworkVersionUtil
{
    private static readonly Dictionary<int, DotNetFrameworkVersion> _map = new()
    {
        { 2, DotNetFrameworkVersion.DotNet2 },
        { 3, DotNetFrameworkVersion.DotNet3 },
        { 5, DotNetFrameworkVersion.DotNet5 },
        { 6, DotNetFrameworkVersion.DotNet6 },
        { 7, DotNetFrameworkVersion.DotNet7 },
        { 8, DotNetFrameworkVersion.DotNet8 },
    };

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

    public static bool IsSdkVersionSupported(SemanticVersion sdkVersion)
    {
        return sdkVersion.Major is 6 or 7;
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
        if (_map.TryGetValue(majorVersion, out var frameworkVersion))
            return frameworkVersion;

        throw new ArgumentOutOfRangeException(nameof(majorVersion), majorVersion, $"Unknown major version: {majorVersion}");
    }

    public static int GetMajorVersion(this DotNetFrameworkVersion frameworkVersion)
    {
        if (!_map.ContainsValue(frameworkVersion))
            throw new ArgumentOutOfRangeException(nameof(frameworkVersion), frameworkVersion, $"Unknown framework version: {frameworkVersion}");

        return _map.First(x => x.Value == frameworkVersion).Key;
    }
}
