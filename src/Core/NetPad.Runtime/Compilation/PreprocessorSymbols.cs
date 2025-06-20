using Microsoft.CodeAnalysis;
using NetPad.DotNet;

namespace NetPad.Compilation;

/// <summary>
/// Application-specific pre-processor symbols.
/// </summary>
public static class PreprocessorSymbols
{
    private static readonly string[] _forDebug = ["NETPAD", "DEBUG", "TRACE"];
    private static readonly string[] _forRelease = ["NETPAD", "RELEASE"];

    public static string[] For(OptimizationLevel optimizationLevel) =>
        optimizationLevel == OptimizationLevel.Debug ? _forDebug : _forRelease;

    public static string[] For(DotNetFrameworkVersion version)
    {
        return ForInternal(version).ToArray();

        IEnumerable<string> ForInternal(DotNetFrameworkVersion version)
        {
            var major = version.GetMajorVersion();

            yield return "NET";
            yield return $"NET{major}_0";

            // Add all past versions symbols
            while (major - 4 > 0)
            {
                yield return $"NET{major}_0_OR_GREATER";
                major--;
            }
        }
    }

    public static string[] For(OptimizationLevel optimizationLevel, DotNetFrameworkVersion version) =>
        For(optimizationLevel).Union(For(version)).ToArray();
}
