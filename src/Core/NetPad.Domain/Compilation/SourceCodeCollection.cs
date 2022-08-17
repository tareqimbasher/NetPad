using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetPad.Compilation;

public class SourceCodeCollection<TSourceCode> : List<TSourceCode> where TSourceCode : SourceCode
{
    public SourceCodeCollection()
    {
    }

    public SourceCodeCollection(IEnumerable<TSourceCode> collection) : base(collection)
    {
    }

    public HashSet<string> GetAllNamespaces()
    {
        return this.SelectMany(s => s.Namespaces).ToHashSet();
    }

    public string GetAllCode()
    {
        var codeBuilder = new StringBuilder();

        foreach (var sourceCode in this)
        {
            codeBuilder.AppendLine(sourceCode.Code);
            codeBuilder.AppendLine();
        }

        return codeBuilder.ToString();
    }

    public string ToParsedSourceCode(bool useGlobalUsings = false)
    {
        var codeBuilder = new StringBuilder();

        string usingPrefix = useGlobalUsings ? "global " : "";

        var usings = GetAllNamespaces()
            .Select(ns => $"{usingPrefix}using {ns};");

        codeBuilder.AppendJoin(Environment.NewLine, usings);
        codeBuilder.AppendLine();
        codeBuilder.AppendLine();

        codeBuilder.AppendLine(GetAllCode());
        return codeBuilder.ToString();
    }
}

public class SourceCodeCollection : SourceCodeCollection<SourceCode>
{
    public SourceCodeCollection()
    {
    }

    public SourceCodeCollection(IEnumerable<SourceCode> collection) : base(collection)
    {
    }
}
