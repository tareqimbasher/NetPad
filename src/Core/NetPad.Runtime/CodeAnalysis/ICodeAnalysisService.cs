using Microsoft.CodeAnalysis;
using NetPad.DotNet;

namespace NetPad.CodeAnalysis;

public interface ICodeAnalysisService
{
    /// <summary>
    /// Gets the full <see cref="SyntaxTree"/> representing a code string.
    /// </summary>
    /// <param name="code">The code string to parse.</param>
    /// <param name="targetFrameworkVersion">The .NET framework version to use to analyze code.</param>
    /// <param name="optimizationLevel">The optimization level to use when parsing code.</param>
    /// <param name="cancellationToken"></param>
    SyntaxTree GetSyntaxTree(
        string code,
        DotNetFrameworkVersion targetFrameworkVersion,
        OptimizationLevel optimizationLevel,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a lighter (slimmer) representation of a syntax tree representing a code string. Useful if planning
    /// to serialize result, as the <see cref="SyntaxTree"/> is deeply nested and huge.
    /// </summary>
    /// <param name="code">The code string to parse.</param>
    /// <param name="targetFrameworkVersion">The .NET framework version to use to analyze code.</param>
    /// <param name="optimizationLevel">The optimization level to use when parsing code.</param>
    /// <param name="cancellationToken"></param>
    /// <returns>The root syntax node (or token when applicable).</returns>
    SyntaxNodeOrTokenSlim GetSyntaxTreeSlim(
        string code,
        DotNetFrameworkVersion targetFrameworkVersion,
        OptimizationLevel optimizationLevel,
        CancellationToken cancellationToken = default);
}
