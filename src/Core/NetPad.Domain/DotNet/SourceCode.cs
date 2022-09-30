using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetPad.DotNet;

public class SourceCode
{
    private readonly HashSet<Using> _usings;
    private bool _changed;

    public SourceCode() : this(code: null, usings: Array.Empty<Using>())
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

    public SourceCode(Code? code, IEnumerable<Using>? usings = null)
    {
        Code = code ?? new Code(null, null);
        _usings = usings?.ToHashSet() ?? new HashSet<Using>();
    }

    public IReadOnlySet<Using> Usings => _usings;
    public Code Code { get; }
    public bool Changed => _changed || Code.Changed || _usings.Any(u => u.Changed);

    public void AddUsing(string @using)
    {
        bool added = _usings.Add(@using);

        if (added) _changed = true;
    }

    public void RemoveUsing(string @using)
    {
        bool removed = _usings.Remove(@using);

        if (removed) _changed = true;
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
}
