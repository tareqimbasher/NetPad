using Microsoft.CodeAnalysis;
using NetPad.DotNet;

namespace NetPad.CodeAnalysis;

public interface ICodeAnalysisService
{
    SyntaxTree GetSyntaxTree(
        string code,
        DotNetFrameworkVersion targetFrameworkVersion,
        OptimizationLevel optimizationLevel,
        CancellationToken cancellationToken = default);

    SyntaxNodeOrTokenSlim GetSyntaxTreeSlim(
        string code,
        DotNetFrameworkVersion targetFrameworkVersion,
        OptimizationLevel optimizationLevel,
        CancellationToken cancellationToken = default);
}
