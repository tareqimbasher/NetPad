using NetPad.Compilation;

namespace NetPad.Data.EntityFrameworkCore.Scaffolding;

public class ScaffoldedSourceFile : SourceCode
{
    public ScaffoldedSourceFile(string className)
    {
        ClassName = className;
    }

    public string ClassName { get; }
    public bool IsDbContext { get; set; }
}
