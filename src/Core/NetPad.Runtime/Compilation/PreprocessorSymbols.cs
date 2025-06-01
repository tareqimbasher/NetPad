using Microsoft.CodeAnalysis;
using NetPad.DotNet;

namespace NetPad.Compilation;

public static class PreprocessorSymbols
{
    public static readonly string[] ForDebug = ["NETPAD", "DEBUG", "TRACE"];
    public static readonly string[] ForRelease = ["NETPAD", "RELEASE"];

    public static string[] For(OptimizationLevel optimizationLevel) =>
        optimizationLevel == OptimizationLevel.Debug ? ForDebug : ForRelease;

    public static string[] For(DotNetFrameworkVersion version)
    {
        var ver = version.GetMajorVersion();
        return ["NET", $"NET{ver}_0", $"NET{ver}_0_OR_GREATER"];
    }

    public static string[] For(OptimizationLevel optimizationLevel, DotNetFrameworkVersion version) => For(optimizationLevel).Union(For(version)).ToArray();
}
