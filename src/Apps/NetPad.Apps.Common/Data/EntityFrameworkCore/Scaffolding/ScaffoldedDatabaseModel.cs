namespace NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;

public class ScaffoldedDatabaseModel(
    ScaffoldedSourceFile dbContextFile,
    ScaffoldedTextFile? dbContextCompiledModelFile)
{
    public ScaffoldedSourceFile DbContextFile { get; } = dbContextFile;

    public ScaffoldedTextFile? DbContextCompiledModelFile { get; } = dbContextCompiledModelFile;
}
