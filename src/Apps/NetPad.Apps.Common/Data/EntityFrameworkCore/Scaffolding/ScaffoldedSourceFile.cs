using NetPad.DotNet;

namespace NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;

public class ScaffoldedSourceFile(string path, string className, string code, IEnumerable<string> usings)
    : SourceCode(code, usings)
{
    public string Path { get; } = path;
    public string ClassName { get; } = className;
    public bool IsDbContext { get; init; }
    public bool IsDbContextCompiledModel { get; init; }
}
