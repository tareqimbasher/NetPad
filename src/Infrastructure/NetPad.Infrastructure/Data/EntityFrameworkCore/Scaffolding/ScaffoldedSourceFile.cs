using System.Collections.Generic;
using NetPad.DotNet;

namespace NetPad.Data.EntityFrameworkCore.Scaffolding;

public class ScaffoldedSourceFile : SourceCode
{
    public ScaffoldedSourceFile(string path, string className, string code, IEnumerable<string> usings)
        : base(code, usings)
    {
        Path = path;
        ClassName = className;
    }

    public string Path { get; }
    public string ClassName { get; }
    public bool IsDbContext { get; init; }
    public bool IsDbContextCompiledModel { get; init; }
}
