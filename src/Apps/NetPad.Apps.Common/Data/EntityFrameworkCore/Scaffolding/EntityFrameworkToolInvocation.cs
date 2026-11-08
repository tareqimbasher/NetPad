namespace NetPad.Apps.Data.EntityFrameworkCore.Scaffolding;

/// <summary>
/// Builds the arguments that run EF Core's design-time tool (<c>ef.dll</c>) against a built project with
/// <c>dotnet exec</c>. The tool runs on the project's own runtime, using the project's dependencies.
/// </summary>
/// <param name="ProjectDirectoryPath">The project's directory.</param>
/// <param name="ProjectName">The project's name.</param>
/// <param name="TargetFrameworkMoniker">The project's target framework, ex. <c>net8.0</c>.</param>
/// <param name="AssemblyPath">The path to the project's built assembly.</param>
/// <param name="NullableSetting">The value of the project's <c>Nullable</c> property.</param>
/// <param name="ToolAssets">The location of the tool.</param>
public sealed record EntityFrameworkToolInvocation(
    string ProjectDirectoryPath,
    string ProjectName,
    string TargetFrameworkMoniker,
    string AssemblyPath,
    string NullableSetting,
    EntityFrameworkToolAssets ToolAssets)
{
    /// <summary>
    /// Gets the <c>dotnet</c> arguments that generate a DbContext and entity types from a database.
    /// </summary>
    /// <param name="connectionString">The connection string of the database.</param>
    /// <param name="providerName">The EF Core provider package name.</param>
    /// <param name="contextName">The name to give the generated DbContext.</param>
    /// <param name="outputDirectoryPath">The directory to generate code in.</param>
    /// <param name="options">The options that adjust what gets generated.</param>
    public IReadOnlyList<string> GetScaffoldArguments(
        string connectionString,
        string providerName,
        string contextName,
        string outputDirectoryPath,
        ScaffoldOptions? options)
    {
        var args = new List<string>
        {
            "dbcontext",
            "scaffold",
            connectionString,
            providerName,
            "--context",
            contextName,
            // Empty so that generated code isn't wrapped in a namespace
            "--namespace",
            "",
            "--force",
            "--output-dir",
            Path.GetRelativePath(ProjectDirectoryPath, outputDirectoryPath),
        };

        if (options?.NoPluralize == true)
        {
            args.Add("--no-pluralize");
        }

        if (options?.UseDatabaseNames == true)
        {
            args.Add("--use-database-names");
        }

        foreach (var schema in options?.Schemas ?? [])
        {
            args.Add("--schema");
            args.Add(schema);
        }

        foreach (var table in options?.Tables ?? [])
        {
            args.Add("--table");
            args.Add(table);
        }

        return WrapInDotNetExec(args);
    }

    /// <summary>
    /// Gets the <c>dotnet</c> arguments that generate a compiled model for the project's DbContext.
    /// </summary>
    /// <param name="outputDirectoryPath">The directory to generate code in.</param>
    public IReadOnlyList<string> GetOptimizeArguments(string outputDirectoryPath)
    {
        return WrapInDotNetExec(
        [
            "dbcontext",
            "optimize",
            "--namespace",
            "",
            "--output-dir",
            Path.GetRelativePath(ProjectDirectoryPath, outputDirectoryPath),
        ]);
    }

    private List<string> WrapInDotNetExec(IEnumerable<string> efCommand)
    {
        var args = new List<string>
        {
            "exec",
            "--depsfile",
            Path.ChangeExtension(AssemblyPath, ".deps.json"),
        };

        foreach (var packageFolder in ToolAssets.PackageFolders)
        {
            args.Add("--additionalprobingpath");
            args.Add(packageFolder);
        }

        args.Add("--runtimeconfig");
        args.Add(Path.ChangeExtension(AssemblyPath, ".runtimeconfig.json"));
        args.Add(ToolAssets.EfDllPath);

        args.AddRange(efCommand);

        args.AddRange(
        [
            "--assembly", AssemblyPath,
            "--project-dir", ProjectDirectoryPath,
            "--root-namespace", ProjectName,
            "--language", "C#",
            "--framework", TargetFrameworkMoniker,
            "--working-dir", ProjectDirectoryPath,
        ]);

        // Unlike the compiler, the tool doesn't see the project's setting. Without this flag it generates non-nullable
        // properties for nullable columns, which compiles but then throws on the first NULL a query reads.
        if (NullableSetting.Equals("enable", StringComparison.OrdinalIgnoreCase)
            || NullableSetting.Equals("annotations", StringComparison.OrdinalIgnoreCase))
        {
            args.Add("--nullable");
        }

        return args;
    }
}
