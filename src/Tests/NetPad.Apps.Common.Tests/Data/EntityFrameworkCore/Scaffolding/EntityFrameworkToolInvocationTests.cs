using NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;

namespace NetPad.Apps.Common.Tests.Data.EntityFrameworkCore.Scaffolding;

public class EntityFrameworkToolInvocationTests
{
    private static readonly string _projectDir = Path.Combine(Path.GetTempPath(), "proj");
    private static readonly string _assembly = Path.Combine(_projectDir, "bin", "Debug", "net8.0", "My.Proj.dll");
    private static readonly string _outputDir = Path.Combine(_projectDir, "DbModel");

    private static EntityFrameworkToolInvocation CreateInvocation(
        string nullable = "enable",
        params string[] packageFolders)
    {
        return new EntityFrameworkToolInvocation(
            _projectDir,
            "My.Proj",
            "net8.0",
            _assembly,
            nullable,
            new EntityFrameworkToolAssets("/pkgs/tools/ef.dll", packageFolders.Length > 0 ? packageFolders : ["/pkgs/"]));
    }

    private static IReadOnlyList<string> Scaffold(
        EntityFrameworkToolInvocation invocation,
        ScaffoldOptions? options = null,
        string connectionString = "Data Source=db")
    {
        return invocation.GetScaffoldArguments(connectionString, "Some.Provider", "MyContext", _outputDir, options);
    }

    private static string[] ValuesOf(IReadOnlyList<string> args, string option)
    {
        return args.Select((arg, index) => (arg, index))
            .Where(x => x.arg == option)
            .Select(x => args[x.index + 1])
            .ToArray();
    }

    [Fact]
    public void Scaffold_ProducesTheFullCommandLine()
    {
        var args = Scaffold(CreateInvocation());

        Assert.Equal(
        [
            "exec",
            "--depsfile", Path.Combine(_projectDir, "bin", "Debug", "net8.0", "My.Proj.deps.json"),
            "--additionalprobingpath", "/pkgs/",
            "--runtimeconfig", Path.Combine(_projectDir, "bin", "Debug", "net8.0", "My.Proj.runtimeconfig.json"),
            "/pkgs/tools/ef.dll",
            "dbcontext", "scaffold", "Data Source=db", "Some.Provider",
            "--context", "MyContext",
            "--namespace", "",
            "--force",
            "--output-dir", "DbModel",
            "--assembly", _assembly,
            "--project-dir", _projectDir,
            "--root-namespace", "My.Proj",
            "--language", "C#",
            "--framework", "net8.0",
            "--working-dir", _projectDir,
            "--nullable",
        ], args);
    }

    [Fact]
    public void Optimize_ProducesTheFullCommandLine()
    {
        var args = CreateInvocation().GetOptimizeArguments(Path.Combine(_outputDir, "CompiledModels"));

        Assert.Equal(
        [
            "exec",
            "--depsfile", Path.Combine(_projectDir, "bin", "Debug", "net8.0", "My.Proj.deps.json"),
            "--additionalprobingpath", "/pkgs/",
            "--runtimeconfig", Path.Combine(_projectDir, "bin", "Debug", "net8.0", "My.Proj.runtimeconfig.json"),
            "/pkgs/tools/ef.dll",
            "dbcontext", "optimize",
            "--namespace", "",
            "--output-dir", Path.Combine("DbModel", "CompiledModels"),
            "--assembly", _assembly,
            "--project-dir", _projectDir,
            "--root-namespace", "My.Proj",
            "--language", "C#",
            "--framework", "net8.0",
            "--working-dir", _projectDir,
            "--nullable",
        ], args);
    }

    [Theory]
    [InlineData("enable", true)]
    [InlineData("Enable", true)]
    [InlineData("annotations", true)]
    [InlineData("disable", false)]
    [InlineData("warnings", false)]
    [InlineData("", false)]
    public void PassesNullableOnlyWhenProjectHasNullableAnnotations(string nullable, bool expected)
    {
        var invocation = CreateInvocation(nullable);

        Assert.Equal(expected, Scaffold(invocation).Contains("--nullable"));
        Assert.Equal(expected, invocation.GetOptimizeArguments(_outputDir).Contains("--nullable"));
    }

    [Fact]
    public void PassesEveryPackageFolderAsProbingPath()
    {
        var args = Scaffold(CreateInvocation("enable", "/cache/", "/fallback/"));

        Assert.Equal(["/cache/", "/fallback/"], ValuesOf(args, "--additionalprobingpath"));
    }

    [Fact]
    public void RepeatsSchemaAndTableForEachValue()
    {
        var args = Scaffold(CreateInvocation(), new ScaffoldOptions
        {
            Schemas = ["public", "my schema"],
            Tables = ["t1", "t2", "t3"]
        });

        Assert.Equal(["public", "my schema"], ValuesOf(args, "--schema"));
        Assert.Equal(["t1", "t2", "t3"], ValuesOf(args, "--table"));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void PassesNamingOptionsOnlyWhenSet(bool noPluralize, bool useDatabaseNames)
    {
        var args = Scaffold(CreateInvocation(), new ScaffoldOptions
        {
            NoPluralize = noPluralize,
            UseDatabaseNames = useDatabaseNames
        });

        Assert.Equal(noPluralize, args.Contains("--no-pluralize"));
        Assert.Equal(useDatabaseNames, args.Contains("--use-database-names"));
    }

    [Theory]
    [InlineData("Host=h;Password=\"quoted\"")]
    [InlineData("Host=h;Password=it's a pass word")]
    public void KeepsConnectionStringAsOneUnalteredArgument(string connectionString)
    {
        var args = Scaffold(CreateInvocation(), connectionString: connectionString);

        Assert.Equal(connectionString, args[args.ToList().IndexOf("scaffold") + 1]);
    }

    [Fact]
    public void PassesEmptyNamespaceAsItsOwnArgument()
    {
        var args = Scaffold(CreateInvocation());

        Assert.Equal([""], ValuesOf(args, "--namespace"));
    }
}
