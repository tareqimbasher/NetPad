using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetPad.Compilation;

public class SourceCode
{
    private readonly HashSet<string> _namespaces;

    public SourceCode() : this(code: null, namespaces: null)
    {
    }

    public SourceCode(IEnumerable<string> namespaces) : this(null, namespaces)
    {
    }

    public SourceCode(params string[] namespaces) : this(null, namespaces)
    {
    }

    public SourceCode(string? code, IEnumerable<string>? namespaces = null)
    {
        Code = code;
        _namespaces = namespaces?
            .Where(ns => !string.IsNullOrWhiteSpace(ns))
            .Select(ns => ns.Trim())
            .ToHashSet() ?? new HashSet<string>();
    }

    public IReadOnlySet<string> Namespaces => _namespaces;
    public string? Code { get; private set; }
    public bool Changed { get; private set; }

    public void AddNamespace(string @namespace)
    {
        _namespaces.Add(@namespace);
        Changed = true;
    }

    public void SetCode(string code)
    {
        Code = code;
        Changed = true;
    }

    public string GetText(bool useGlobalUsings = false)
    {
        var builder = new StringBuilder();

        string usingPrefix = useGlobalUsings ? "global " : "";

        builder
            .AppendJoin(Environment.NewLine, Namespaces.Select(ns => $"{usingPrefix}using {ns};"))
            .AppendLine();

        if (Code != null)
        {
            builder.AppendLine();
            builder.AppendLine(Code);
        }

        return builder.ToString();
    }
}
