using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace NetPad.CodeAnalysis;

public class SyntaxTriviaSlim
{
    public SyntaxTriviaSlim(SyntaxKind kind, LinePositionSpan span, string? displayValue = null)
    {
        Kind = kind;
        Span = span;
        DisplayValue = displayValue.Truncate(50, true);
    }

    public SyntaxKind Kind { get; }
    public LinePositionSpan Span { get; }
    public string? DisplayValue { get; }
}
