using System.Collections.Generic;
using System.Linq;

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
        Namespaces = namespaces?.ToHashSet() ?? new HashSet<string>();
    }

    public HashSet<string> Namespaces { get; set; }
    public string? Code { get; set; }
}
