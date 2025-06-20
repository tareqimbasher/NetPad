using NetPad.Data.Metadata;
using NetPad.DotNet;

namespace NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;

public class ScaffoldResult(ScaffoldedDatabaseModel model, AssemblyImage assembly, DatabaseStructure? databaseStructure)
{
    public ScaffoldedDatabaseModel Model { get; } = model;
    public AssemblyImage Assembly { get; } = assembly;
    public DatabaseStructure? DatabaseStructure { get; } = databaseStructure;
}
