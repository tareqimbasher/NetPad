using NetPad.DotNet;

namespace NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;

public class ScaffoldedDatabaseModel
{
    public SourceCodeCollection<ScaffoldedSourceFile> SourceFiles { get; } = [];

    public ScaffoldedSourceFile DbContextFile => SourceFiles.Single(f => f.IsDbContext);

    public ScaffoldedSourceFile? DbContextCompiledModelFile => SourceFiles.FirstOrDefault(f => f.IsDbContextCompiledModel);

    public void AddFile(ScaffoldedSourceFile file)
    {
        if (file.IsDbContext && SourceFiles.Any(f => f.IsDbContext))
            throw new ArgumentException("A db context source file already exists.");

        if (file.IsDbContext)
            SourceFiles.Insert(0, file);
        else
            SourceFiles.Add(file);
    }
}
