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
        return ForInternal(version).ToArray();

        IEnumerable<string> ForInternal(DotNetFrameworkVersion version)
        {
            var ver = version.GetMajorVersion();

            yield return "NET";
            yield return  $"NET{ver}_0";

            // Add all past versions symbols
            while (ver - 4 > 0)
            {
                yield return $"NET{ver}_0_OR_GREATER";
                ver--;
            }
        }
    }

    public static string[] For(OptimizationLevel optimizationLevel, DotNetFrameworkVersion version) => For(optimizationLevel).Union(For(version)).ToArray();
}
