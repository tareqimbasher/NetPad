using Microsoft.CodeAnalysis;

namespace NetPad.Compilation;

public static class PreprocessorSymbols
{
    public static readonly string[] ForDebug = { "NETPAD", "DEBUG", "TRACE" };
    public static readonly string[] ForRelease = { "NETPAD", "RELEASE" };

    public static string[] For(OptimizationLevel optimizationLevel) => optimizationLevel == OptimizationLevel.Debug
        ? PreprocessorSymbols.ForDebug
        : PreprocessorSymbols.ForRelease;
}
