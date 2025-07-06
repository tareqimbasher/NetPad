using NetPad.DotNet.CodeAnalysis;

namespace NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;

public class ScaffoldedDatabaseModel(
    SourceCodeCollection<ScaffoldedSourceFile> sourceFiles,
    ScaffoldedSourceFile dbContextFile,
    ScaffoldedSourceFile? dbContextCompiledModelFile)
{
    public SourceCodeCollection<ScaffoldedSourceFile> SourceFiles { get; } = sourceFiles;

    public ScaffoldedSourceFile DbContextFile { get; } = dbContextFile;

    public ScaffoldedSourceFile? DbContextCompiledModelFile { get; } = dbContextCompiledModelFile;
}
