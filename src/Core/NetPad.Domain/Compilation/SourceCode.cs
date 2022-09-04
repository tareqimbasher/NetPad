using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetPad.Compilation;

public class SourceCode
{
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
        Namespaces = namespaces?
            .Where(ns => !string.IsNullOrWhiteSpace(ns))
            .Select(ns => ns.Trim())
            .ToHashSet() ?? new HashSet<string>();
    }

    public HashSet<string> Namespaces { get; set; }
    public string? Code { get; set; }

    public string GetText(bool useGlobalUsings = false)
    {
        var builder = new StringBuilder();

        string usingPrefix = useGlobalUsings ? "global " : "";

        builder.AppendJoin(Environment.NewLine, Namespaces.Select(ns => $"{usingPrefix}using {ns};"));

        if (Code != null)
        {
            builder.AppendLine();
            builder.AppendLine(Code);
        }

        return builder.ToString();
    }
}
