using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace NetPad.DotNet;

public class SourceCode
{
    private readonly HashSet<Using> _usings;
    private bool _valueChanged;

    public SourceCode() : this(null, Array.Empty<Using>())
    {
    }

    public SourceCode(IEnumerable<string> usings) : this(null, usings)
    {
    }

    public SourceCode(params string[] usings) : this(null, usings)
    {
    }

    public SourceCode(IEnumerable<Using> usings) : this(null, usings)
    {
    }

    public SourceCode(params Using[] usings) : this(null, usings)
    {
    }

    public SourceCode(string? code, IEnumerable<string>? usings = null)
        : this(
            code == null ? null : new Code(code),
            usings?.Select(u => new Using(u))
        )
    {
    }

    [JsonConstructor]
    public SourceCode(Code? code, IEnumerable<Using>? usings = null)
    {
        Code = code ?? new Code(null, null);
        _usings = usings?.ToHashSet() ?? new HashSet<Using>();
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
        var syntaxTreeRoot = CSharpSyntaxTree.ParseText(text).GetRoot();
        var nodes = syntaxTreeRoot.DescendantNodes().ToArray();

        var usings = new HashSet<string>();
        var usingSpans = new List<(int startIndex, int length)>();

        foreach (var usingDirective in nodes.OfType<UsingDirectiveSyntax>())
        {
            int startIndex = usingDirective.Span.Start;
            int length = usingDirective.Span.Length;

            usingSpans.Add((startIndex, length));

            var ns = text.Substring(startIndex, length)
                .Split(' ').Skip(1).JoinToString(" ")
                .TrimEnd(';');

            usings.Add(ns);
        }

        string code;

        if (!usings.Any())
        {
            code = text;
        }
        else
        {
            usingSpans = usingSpans.OrderBy(s => s.startIndex).ToList();
            code = text.RemoveRanges(usingSpans);
        }

        return new SourceCode(code, usings);
    }
}
