using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace NetPad.CodeAnalysis;

public class SyntaxNodeOrTokenSlim
{
    public SyntaxNodeOrTokenSlim(bool isToken, bool isNode, SyntaxKind kind, LinePositionSpan span, bool isMissing)
    {
        IsToken = isToken;
        IsNode = isNode;
        Kind = kind;
        Span = span;
        IsMissing = isMissing;
        LeadingTrivia = new();
        TrailingTrivia = new();
        Children = new();
    }

    public bool IsToken { get; }
    public bool IsNode { get; }
    public SyntaxKind Kind { get; }
    public LinePositionSpan Span { get; }
    public bool IsMissing { get; }
    public string? ValueText { get; private set; }
    public List<SyntaxTriviaSlim> LeadingTrivia { get; }
    public List<SyntaxTriviaSlim> TrailingTrivia { get; }
    public List<SyntaxNodeOrTokenSlim> Children { get; }

    public void SetValueText(string text)
    {
        ValueText = text;
    }
}
