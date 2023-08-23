using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using NetPad.Compilation;
using NetPad.Configuration;
using NetPad.Data.EntityFrameworkCore.DataConnections;
using NetPad.Data.EntityFrameworkCore.Scaffolding;
using NetPad.DotNet;
using NetPad.Packages;
using NetPad.Utilities;

namespace NetPad.Data.EntityFrameworkCore;

public class EntityFrameworkResourcesGenerator : IDataConnectionResourcesGenerator
{
    private readonly Settings _settings;
    private readonly ILoggerFactory _loggerFactory;
    private readonly Lazy<IDataConnectionResourcesCache> _dataConnectionResourcesCache;
    private readonly ICodeCompiler _codeCompiler;
    private readonly IPackageProvider _packageProvider;
    private readonly IDataConnectionPasswordProtector _dataConnectionPasswordProtector;
    private readonly IDotNetInfo _dotNetInfo;

    public EntityFrameworkResourcesGenerator(
        Lazy<IDataConnectionResourcesCache> dataConnectionResourcesCache,
        ICodeCompiler codeCompiler,
        IPackageProvider packageProvider,
        IDataConnectionPasswordProtector dataConnectionPasswordProtector,
        IDotNetInfo dotNetInfo,
        Settings settings,
        ILoggerFactory loggerFactory)
    {
        _dataConnectionResourcesCache = dataConnectionResourcesCache;
        _codeCompiler = codeCompiler;
        _packageProvider = packageProvider;
        _dataConnectionPasswordProtector = dataConnectionPasswordProtector;
        _dotNetInfo = dotNetInfo;
        _settings = settings;
        _loggerFactory = loggerFactory;
    }

    public async Task<DataConnectionSourceCode> GenerateSourceCodeAsync(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion)
    {
        if (!dataConnection.IsEntityFrameworkDataConnection(out var efDbConnection))
        {
            return new DataConnectionSourceCode();
        }

        var scaffolder = new EntityFrameworkDatabaseScaffolder(
            targetFrameworkVersion,
            efDbConnection,
            _dataConnectionPasswordProtector,
            _dotNetInfo,
            _settings,
            _loggerFactory.CreateLogger<EntityFrameworkDatabaseScaffolder>());

        var model = await scaffolder.ScaffoldAsync();

        var applicationCode = GenerateApplicationCode(model);

        return new DataConnectionSourceCode
        {
            DataAccessCode = new SourceCodeCollection(model.SourceFiles),
            ApplicationCode = applicationCode
        };
    }

    public async Task<AssemblyImage?> GenerateAssemblyAsync(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion)
    {
        if (!dataConnection.IsEntityFrameworkDataConnection(out var efDbConnection))
        {
            return null;
        }

        var sourceCode = await _dataConnectionResourcesCache.Value.GetSourceGeneratedCodeAsync(dataConnection, targetFrameworkVersion);

        if (!sourceCode.DataAccessCode.Any())
        {
            return null;
        }

        var result = await CompileNewAssemblyAsync(targetFrameworkVersion, efDbConnection, sourceCode.DataAccessCode);

        if (!result.Success)
        {
            throw new Exception("Could not compile data connection assembly. " +
                                $"Compilation failed with the following diagnostics: \n{string.Join("\n", result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))}");
        }

        return new AssemblyImage(result.AssemblyName, result.AssemblyBytes);
    }

    public async Task<Reference[]> GetRequiredReferencesAsync(DataConnection dataConnection, DotNetFrameworkVersion targetFrameworkVersion)
    {
        if (!dataConnection.IsEntityFrameworkDataConnection(out var efDbConnection))
            return Array.Empty<Reference>();

        var references = new List<Reference>();

        references.Add(new PackageReference(
            efDbConnection.EntityFrameworkProviderName,
            efDbConnection.EntityFrameworkProviderName,
            await EntityFrameworkPackageUtils.GetEntityFrameworkProviderVersionAsync(targetFrameworkVersion, efDbConnection.EntityFrameworkProviderName)
            ?? throw new Exception($"Could not find a version for entity framework provider: '{efDbConnection.EntityFrameworkProviderName}'")
        ));

        return references.ToArray();
    }

    private async Task<CompilationResult> CompileNewAssemblyAsync(
        DotNetFrameworkVersion targetFrameworkVersion,
        EntityFrameworkDatabaseConnection efConnection,
        SourceCodeCollection sourceCode)
    {
        var assemblyFileReferences = new HashSet<string>();

        var requiredReferences = await GetRequiredReferencesAsync(efConnection, targetFrameworkVersion);
        assemblyFileReferences.AddRange((await requiredReferences.GetAssetsAsync(targetFrameworkVersion, _packageProvider))
            .Where(a => a.IsAssembly())
            .Select(a => a.Path));

        var code = sourceCode.ToCodeString();

        return _codeCompiler.Compile(new CompilationInput(
                code,
                targetFrameworkVersion,
                null,
                assemblyFileReferences)
            .WithOutputKind(OutputKind.DynamicallyLinkedLibrary)
            .WithOutputAssemblyNameTag($"data-connection_{efConnection.Id}")
        );
    }

    private SourceCodeCollection GenerateApplicationCode(ScaffoldedDatabaseModel model)
    {
        // We want to add utility code to a partial Program class that can be used to augment the Program class in scripts.
        // The goal is to accomplish the following items, mainly for convenience while writing scripts:
        // 1. Make the Program class inherit the generated DbContext
        //    Why? - This allows users to override methods on the base DbContext, ex: the OnConfiguring(DbContextOptionsBuilder optionsBuilder) method.
        //
        // 2. Add a a property for the generated DbContext
        //    Why? - This allows users to access the DbContext instance being used in the script
        //
        // 3. Add properties for all the generated DbSet's
        //    Why? - This makes it easy for users to just type in the name of the table/DbSet (ex: "Authors") in their query
        //           instead of having to do something like "DbContext.Authors"

        var code = new StringBuilder();

        // 1. Make the Program class inherit the generated DbContext
        // The program class here will merge with the Program class generated
        // in our Script top-level program
        var dbContext = model.DbContextFile;
        code.AppendLine($"public partial class Program : {dbContext.ClassName}<Program>")
            .AppendLine(@"
{
    public Program()
    {
    }

    public Program(DbContextOptions<Program> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .EnableSensitiveDataLogging()
            .LogTo(
                output =>
                {
                    if (!output.Contains(""Executing DbCommand"")) return;

                    ScriptRuntimeServices.SqlWrite(output + ""\n"");
                },
                new[] { Microsoft.EntityFrameworkCore.DbLoggerCategory.Database.Command.Name }
            );

        base.OnConfiguring(optionsBuilder);
    }

    /// <summary>
    /// Calls DataContext.SaveChanges();
    /// </summary>
    public static int SaveChanges()
    {
        return DataContext.SaveChanges();
    }

    /// <summary>
    /// Calls DataContext.SaveChangesAsync(CancellationToken cancellationToken);
    /// </summary>
    public static Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return DataContext.SaveChangesAsync(cancellationToken);
    }
");

        // 2. Add the DbContext property
        code
            .AppendLine($@"
    private static Program? _program;

    /// <summary>
    /// The DbContext instance used to access the database.
    /// </summary>
    public static {dbContext.ClassName}<Program> DataContext => _program ??= new Program();
");


        var dbContextCodeLines = dbContext.Code.Value!.Split(Environment.NewLine).ToList();

        // 3. Add properties for all the generated DbSet's
        var programProperties = new List<string>();

        for (var iLine = 0; iLine < dbContextCodeLines.Count; iLine++)
        {
            var line = dbContextCodeLines[iLine];

            // We only need DbSet property lines
            if (!line.Contains("public virtual DbSet<")) continue;

            // Extracts 'DbSet<Book> Books' from 'public virtual DbSet<Book> Books { get; set; } = null!;'
            var typeAndName = line.SubstringBetween("virtual ", " {");
            var parts = typeAndName.Split(" ");

            var entityType = parts[0].SubstringBetween("<", ">");
            var propertyName = parts[1];
            var dbContextPropertyName = $"{propertyName}_HIDDEN";

            programProperties.Add($@"
    /// <summary>
    /// The {propertyName} table (DbSet).
    /// </summary>
    public static Microsoft.EntityFrameworkCore.DbSet<{entityType}> {propertyName} => DataContext.{dbContextPropertyName};");

            // Rename property on DbContext
            dbContextCodeLines[iLine] = line.Replace($" {propertyName} ", $" {dbContextPropertyName} ");
        }

        // Replace the DbContext code since we modified it above when renaming properties
        dbContext.Code.Update(dbContextCodeLines.JoinToString(Environment.NewLine));

        code.AppendJoin(Environment.NewLine, programProperties)
            .AppendLine()
            .AppendLine("}");

        var applicationCode = new SourceCode(code.ToString());
        applicationCode.AddUsing("Microsoft.EntityFrameworkCore");

        return new SourceCodeCollection(new[] { applicationCode });
    }
}
