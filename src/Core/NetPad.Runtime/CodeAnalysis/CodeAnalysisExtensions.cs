using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetPad.CodeAnalysis;

public static class CodeAnalysisExtensions
{
    /// <summary>
    /// Gets the namespace value specified by the using directive.
    /// </summary>
    public static string GetNamespaceString(this UsingDirectiveSyntax node)
    {
        return string.Join(' ',
            node.ChildNodes()
                .Select(x => x.NormalizeWhitespace().ToFullString()));
    }
}
