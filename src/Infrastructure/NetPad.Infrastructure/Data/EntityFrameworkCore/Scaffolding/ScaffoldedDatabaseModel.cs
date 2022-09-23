using System;
using System.Linq;
using NetPad.Compilation;

namespace NetPad.Data.EntityFrameworkCore.Scaffolding;

public class ScaffoldedDatabaseModel
{
    private readonly SourceCodeCollection<ScaffoldedSourceFile> _sourceFiles;

    public ScaffoldedDatabaseModel()
    {
        _sourceFiles = new SourceCodeCollection<ScaffoldedSourceFile>();
    }

    public SourceCodeCollection<ScaffoldedSourceFile> SourceFiles => _sourceFiles;

    public ScaffoldedSourceFile DbContextFile => _sourceFiles.Single(f => f.IsDbContext);

    public void AddFile(ScaffoldedSourceFile file)
    {
        if (file.IsDbContext && _sourceFiles.Any(f => f.IsDbContext))
            throw new ArgumentException("A db context source file already exists.");

        if (file.IsDbContext)
            _sourceFiles.Insert(0, file);
        else
            _sourceFiles.Add(file);
    }
}
