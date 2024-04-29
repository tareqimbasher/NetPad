using System;
using System.Linq;
using NetPad.DotNet;

namespace NetPad.Data.EntityFrameworkCore.Scaffolding;

public class ScaffoldedDatabaseModel
{
    public ScaffoldedDatabaseModel()
    {
        SourceFiles = new SourceCodeCollection<ScaffoldedSourceFile>();
    }

    public SourceCodeCollection<ScaffoldedSourceFile> SourceFiles { get; }

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
