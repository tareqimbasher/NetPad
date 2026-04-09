namespace NetPad.Apps.Mcp.Tools;

internal static class ScriptValidation
{
    public static readonly HashSet<string> ValidKinds = new(StringComparer.OrdinalIgnoreCase) { "Program", "SQL" };

    public static readonly HashSet<string> ValidOptimizationLevels = new(StringComparer.OrdinalIgnoreCase)
        { "Debug", "Release" };
}
