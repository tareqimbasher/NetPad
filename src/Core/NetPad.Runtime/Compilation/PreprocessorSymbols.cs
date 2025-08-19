using Microsoft.CodeAnalysis;
using NetPad.DotNet;

namespace NetPad.Compilation;

/// <summary>
/// Gets pre-processor symbols supported in user script code.
/// </summary>
public static class PreprocessorSymbols
{
    private static readonly string[] _debugSymbols = ["NETPAD", "DEBUG", "TRACE"];
    private static readonly string[] _releaseSymbols = ["NETPAD", "RELEASE"];

    public static string[] For(OptimizationLevel optimizationLevel) =>
        (optimizationLevel == OptimizationLevel.Debug ? _debugSymbols : _releaseSymbols).ToArray();

    public static string[] For(DotNetFrameworkVersion version)
    {
        var major = version.GetMajorVersion();

        var symbols = new List<string>
        {
            "NETPAD",
            "NET",
            $"NET{major}_0",
        };

        // Add all past versions symbols
        while (major >= 5)
        {
            symbols.Add($"NET{major}_0_OR_GREATER");
            major--;
        }

        return symbols.ToArray();
    }

    public static string[] For(OptimizationLevel optimizationLevel, DotNetFrameworkVersion version) =>
        For(optimizationLevel).Union(For(version)).ToArray();
}
