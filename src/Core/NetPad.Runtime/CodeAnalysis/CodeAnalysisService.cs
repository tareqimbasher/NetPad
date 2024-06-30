using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using NetPad.Compilation;
using NetPad.DotNet;

namespace NetPad.CodeAnalysis;

public class CodeAnalysisService : ICodeAnalysisService
{
    internal CSharpParseOptions GetParseOptions(
        DotNetFrameworkVersion targetFrameworkVersion,
        OptimizationLevel optimizationLevel)
    {
        return CSharpParseOptions.Default
            .WithLanguageVersion(targetFrameworkVersion.GetLatestSupportedCSharpLanguageVersion())
            // TODO investigate using SourceCodeKind.Script (see cs-scripts branch)
            .WithKind(SourceCodeKind.Regular)
            .WithPreprocessorSymbols(PreprocessorSymbols.For(optimizationLevel));
    }

    public SyntaxTree GetSyntaxTree(
        string code,
        DotNetFrameworkVersion targetFrameworkVersion,
        OptimizationLevel optimizationLevel,
        CancellationToken cancellationToken = default)
    {
        var sourceText = SourceText.From(code);
        var parseOptions = GetParseOptions(targetFrameworkVersion, optimizationLevel);

        return SyntaxFactory.ParseSyntaxTree(sourceText, parseOptions, cancellationToken: cancellationToken);
    }

    public SyntaxNodeOrTokenSlim GetSyntaxTreeSlim(
        string code,
        DotNetFrameworkVersion targetFrameworkVersion,
        OptimizationLevel optimizationLevel,
        CancellationToken cancellationToken = default)
    {
        var syntaxTree = GetSyntaxTree(code, targetFrameworkVersion, optimizationLevel, cancellationToken);

        var root = syntaxTree.GetRoot(cancellationToken);

        return Build(root, cancellationToken);
    }

    private static SyntaxNodeOrTokenSlim Build(SyntaxNodeOrToken nodeOrToken, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var kind = nodeOrToken.Kind();

        var element = new SyntaxNodeOrTokenSlim(
            nodeOrToken.IsToken,
            nodeOrToken.IsNode,
            kind,
            nodeOrToken.IsNode ? nodeOrToken.AsNode()!.GetType().Name : string.Empty,
            nodeOrToken.GetLocation()!.GetLineSpan().Span,
            nodeOrToken.IsMissing);

        if (nodeOrToken.IsToken)
        {
            element.SetValueText(nodeOrToken.AsToken().ValueText.Truncate(50, true));
        }

        if (nodeOrToken.HasLeadingTrivia)
        {
            element.LeadingTrivia.AddRange(nodeOrToken.GetLeadingTrivia().Select(ToTrivia));
        }

        if (nodeOrToken.HasTrailingTrivia)
        {
            element.TrailingTrivia.AddRange(nodeOrToken.GetTrailingTrivia().Select(ToTrivia));
        }

        foreach (var child in nodeOrToken.ChildNodesAndTokens())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var childElement = Build(child, cancellationToken);
            element.Children.Add(childElement);
        }

        return element;
    }

    private static SyntaxTriviaSlim ToTrivia(SyntaxTrivia trivia)
    {
        var kind = trivia.Kind();

        if (!_triviaDisplayValues.TryGetValue(trivia.Kind(), out var displayValue))
        {
            if (trivia.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                var length = trivia.Span.Length;
                displayValue = length > 1 ? $"<space:{length}>" : "<space>";
            }
            else
            {
                displayValue = kind.ToString();
            }
        }

        return new SyntaxTriviaSlim(kind, trivia.GetLocation()!.GetLineSpan().Span, displayValue.Truncate(50, true));
    }

    private static readonly Dictionary<SyntaxKind, string> _triviaDisplayValues = new Dictionary<SyntaxKind, string>
    {
        [SyntaxKind.SemicolonToken] = ";",
        [SyntaxKind.EndOfLineTrivia] = "\\n",
        [SyntaxKind.WhitespaceTrivia] = "<space>",
        [SyntaxKind.SingleLineCommentTrivia] = "<comment>",
        [SyntaxKind.MultiLineCommentTrivia] = "<comment>",
        [SyntaxKind.DocumentationCommentExteriorTrivia] = "<comment>",
        [SyntaxKind.SingleLineDocumentationCommentTrivia] = "<comment>",
        [SyntaxKind.MultiLineDocumentationCommentTrivia] = "<comment>",
        [SyntaxKind.XmlComment] = "<comment>",
        [SyntaxKind.PublicKeyword] = "public",
        [SyntaxKind.VoidKeyword] = "void",
    };
}
