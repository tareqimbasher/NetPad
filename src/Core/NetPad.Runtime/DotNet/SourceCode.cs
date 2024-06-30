using System.Text;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NetPad.CodeAnalysis;

namespace NetPad.DotNet;

public class SourceCode
{
    private readonly HashSet<Using> _usings;
    private bool _valueChanged;

    [JsonConstructor]
    public SourceCode(Code? code, IEnumerable<Using>? usings = null)
    {
        Code = code ?? new Code(null);
        _usings = usings?.ToHashSet() ?? [];
    }

    public SourceCode(IEnumerable<Using> usings) : this(null, usings)
    {
    }

    public SourceCode(string? code, IEnumerable<string>? usings = null)
        : this(code == null ? null : new Code(code), usings?.Select(u => new Using(u)))
    {
    }

    public SourceCode(IEnumerable<string> usings) : this(null, usings)
    {
    }

    public IEnumerable<Using> Usings => _usings;
    public Code Code { get; }
    public bool ValueChanged() => _valueChanged || Code.ValueChanged() || _usings.Any(u => u.ValueChanged());

    public void AddUsing(string @using)
    {
        bool added = _usings.Add(@using);

        if (added) _valueChanged = true;
    }

    public void RemoveUsing(string @using)
    {
        bool removed = _usings.Remove(@using);

        if (removed) _valueChanged = true;
    }

    public string ToCodeString(bool useGlobalNotation = false)
    {
        var builder = new StringBuilder();

        builder
            .AppendJoin(Environment.NewLine, Usings.Select(ns => ns.ToCodeString(useGlobalNotation)))
            .AppendLine();

        builder.AppendLine();
        builder.AppendLine(Code.ToCodeString());

        return builder.ToString();
    }

    public static SourceCode Parse(string text)
    {
        var root = CSharpSyntaxTree.ParseText(text).GetRoot();

        var usingDirectives = root
            .DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .ToArray();

        var usings = usingDirectives
            .Select(u => u.GetNamespaceString())
            .ToArray();

        var code = root
            .RemoveNodes(usingDirectives, SyntaxRemoveOptions.KeepNoTrivia)?
            .NormalizeWhitespace()
            .ToFullString();

        return new SourceCode(code, usings);
    }
}
