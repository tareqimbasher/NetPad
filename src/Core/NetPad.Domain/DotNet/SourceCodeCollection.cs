using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetPad.DotNet;

public class SourceCodeCollection<TSourceCode> : List<TSourceCode> where TSourceCode : SourceCode
{
    public SourceCodeCollection()
    {
    }

    public SourceCodeCollection(IEnumerable<TSourceCode> collection) : base(collection)
    {
    }

    public HashSet<Using> GetAllUsings()
    {
        return this.SelectMany(s => s.Usings).ToHashSet();
    }

    public string GetAllCode()
    {
        var codeBuilder = new StringBuilder();

        foreach (var sourceCode in this)
        {
            codeBuilder.AppendLine(sourceCode.Code.ToCodeString());
            codeBuilder.AppendLine();
        }

        return codeBuilder.ToString();
    }

    public string ToCodeString(bool useGlobalUsingNotation = false)
    {
        var codeBuilder = new StringBuilder();

        var usings = GetAllUsings()
            .Select(u => u.ToCodeString(useGlobalUsingNotation));

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
