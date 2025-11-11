using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace NetPad.DotNet;

public static class DotNetFrameworkVersionUtil
{
    public const int MinSupportedDotNetVersion = 6;
    public const int MaxSupportedDotNetVersion = 10;
    public const int MinSupportedEfToolVersion = 5;

    private static readonly Dictionary<int, DotNetFrameworkVersion> _majorToFrameworkVersion = new()
    {
        { 5, DotNetFrameworkVersion.DotNet5 },
        { 6, DotNetFrameworkVersion.DotNet6 },
        { 7, DotNetFrameworkVersion.DotNet7 },
        { 8, DotNetFrameworkVersion.DotNet8 },
        { 9, DotNetFrameworkVersion.DotNet9 },
        { 10, DotNetFrameworkVersion.DotNet10 },
    };

    private static readonly Dictionary<DotNetFrameworkVersion, int> _frameworkVersionToMajor =
        _majorToFrameworkVersion.ToDictionary(kv => kv.Value, kv => kv.Key);

    private static readonly Dictionary<DotNetFrameworkVersion, string> _frameworkVersionToTfm = new()
    {
        { DotNetFrameworkVersion.DotNet5, "net5.0" },
        { DotNetFrameworkVersion.DotNet6, "net6.0" },
        { DotNetFrameworkVersion.DotNet7, "net7.0" },
        { DotNetFrameworkVersion.DotNet8, "net8.0" },
        { DotNetFrameworkVersion.DotNet9, "net9.0" },
        { DotNetFrameworkVersion.DotNet10, "net10.0" },
    };

    private static readonly Dictionary<string, DotNetFrameworkVersion> _tfmToFrameworkVersion =
        _frameworkVersionToTfm.ToDictionary(kv => kv.Value, kv => kv.Key);

    private static readonly Dictionary<DotNetFrameworkVersion, LanguageVersion> _frameworkVersionToLangVersion = new()
    {
        { DotNetFrameworkVersion.DotNet5, LanguageVersion.CSharp9 },
        { DotNetFrameworkVersion.DotNet6, LanguageVersion.CSharp10 },
        { DotNetFrameworkVersion.DotNet7, LanguageVersion.CSharp11 },
        { DotNetFrameworkVersion.DotNet8, LanguageVersion.CSharp12 },
        { DotNetFrameworkVersion.DotNet9, LanguageVersion.CSharp13 },
        { DotNetFrameworkVersion.DotNet10, LanguageVersion.Preview },
    };


    public static bool IsSdkVersionSupported(SemanticVersion sdkVersion)
    {
        return sdkVersion.Major is >= MinSupportedDotNetVersion and <= MaxSupportedDotNetVersion;
    }

    public static bool IsSupported(this DotNetSdkVersion sdkVersion)
    {
        return IsSdkVersionSupported(sdkVersion.Version);
    }

    public static bool IsEfToolVersionSupported(SemanticVersion efToolVersion)
    {
        return efToolVersion.Major >= MinSupportedEfToolVersion;
    }


    public static string GetTargetFrameworkMoniker(this DotNetFrameworkVersion frameworkVersion)
    {
        return _frameworkVersionToTfm.TryGetValue(frameworkVersion, out var tfm)
            ? tfm
            : throw new ArgumentOutOfRangeException(nameof(frameworkVersion), frameworkVersion, $"Unknown framework version: {frameworkVersion}");
    }

    public static DotNetFrameworkVersion GetFrameworkVersion(string targetFrameworkMoniker)
    {
        return TryGetFrameworkVersion(targetFrameworkMoniker, out var frameworkVersion)
            ? frameworkVersion.Value
            : throw new ArgumentOutOfRangeException(
                nameof(targetFrameworkMoniker),
                targetFrameworkMoniker,
                $"Unknown target framework moniker (TFM): {targetFrameworkMoniker}");
    }

    public static bool TryGetFrameworkVersion(string targetFrameworkMoniker, [NotNullWhen(true)] out DotNetFrameworkVersion? frameworkVersion)
    {
        if (_tfmToFrameworkVersion.TryGetValue(targetFrameworkMoniker, out var version))
        {
            frameworkVersion = version;
            return true;
        }

        frameworkVersion = null;
        return false;
    }

    public static DotNetFrameworkVersion GetFrameworkVersion(this DotNetRuntimeVersion runtimeVersion)
    {
        return GetFrameworkVersion(runtimeVersion.Version.Major);
    }

    public static DotNetFrameworkVersion GetFrameworkVersion(this DotNetSdkVersion sdkVersion)
    {
        return GetFrameworkVersion(sdkVersion.Version.Major);
    }

    public static DotNetFrameworkVersion GetFrameworkVersion(int majorVersion)
    {
        if (_majorToFrameworkVersion.TryGetValue(majorVersion, out var frameworkVersion))
            return frameworkVersion;

        throw new ArgumentOutOfRangeException(nameof(majorVersion), majorVersion, $"Unknown major version: {majorVersion}");
    }

    public static int GetMajorVersion(this DotNetFrameworkVersion frameworkVersion)
    {
        if (!_frameworkVersionToMajor.TryGetValue(frameworkVersion, out int majorVersion))
            throw new ArgumentOutOfRangeException(nameof(frameworkVersion), frameworkVersion, $"Unknown framework version: {frameworkVersion}");

        return majorVersion;
    }

    public static LanguageVersion GetLatestSupportedCSharpLanguageVersion(this DotNetFrameworkVersion dotNetFrameworkVersion)
    {
        return _frameworkVersionToLangVersion.TryGetValue(dotNetFrameworkVersion, out var languageVersion)
            ? languageVersion
            : throw new ArgumentOutOfRangeException(
                nameof(dotNetFrameworkVersion),
                dotNetFrameworkVersion,
                $"Unknown framework version: {dotNetFrameworkVersion}");
    }
}
