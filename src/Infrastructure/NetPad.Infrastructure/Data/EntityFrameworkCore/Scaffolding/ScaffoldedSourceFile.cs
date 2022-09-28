using System.Collections.Generic;
using NetPad.Compilation;

namespace NetPad.Data.EntityFrameworkCore.Scaffolding;

public class ScaffoldedSourceFile : SourceCode
{
    public ScaffoldedSourceFile(string path, string className, string code, IEnumerable<string> namespaces)
        : base(code, namespaces)
    {
        Path = path;
        ClassName = className;
    }

    public string Path { get; }
    public string ClassName { get; }
    public bool IsDbContext { get; set; }
}
