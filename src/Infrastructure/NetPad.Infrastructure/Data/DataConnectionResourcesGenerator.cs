using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using NetPad.Compilation;
using NetPad.Configuration;
using NetPad.Data.Scaffolding;
using NetPad.DotNet;
using NetPad.Packages;
using NetPad.Utilities;

namespace NetPad.Data;

public class DataConnectionResourcesGenerator : IDataConnectionResourcesGenerator
{
    private readonly Settings _settings;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ICodeCompiler _codeCompiler;
    private readonly IPackageProvider _packageProvider;

    public DataConnectionResourcesGenerator(
        ICodeCompiler codeCompiler,
        IPackageProvider packageProvider,
        Settings settings,
        ILoggerFactory loggerFactory)
    {
        _codeCompiler = codeCompiler;
        _packageProvider = packageProvider;
        _settings = settings;
        _loggerFactory = loggerFactory;
    }

    public async Task<SourceCodeCollection> GenerateSourceCodeAsync(DataConnection dataConnection)
    {
        if (dataConnection is not EntityFrameworkDatabaseConnection efDbConnection)
        {
            return new SourceCodeCollection();
        }

        var scaffolder = new EntityFrameworkDatabaseScaffolder(
            efDbConnection,
            _packageProvider,
            _settings,
            _loggerFactory.CreateLogger<EntityFrameworkDatabaseScaffolder>());

        var model = await scaffolder.ScaffoldAsync();

        var code = new SourceCodeCollection(model.SourceFiles);

        code.Add(GenerateUtilityCode(model));

        return code;
    }

    public async Task<byte[]?> GenerateAssemblyAsync(DataConnection dataConnection, SourceCodeCollection sourceCode)
    {
        if (dataConnection is not EntityFrameworkDatabaseConnection efDbConnection)
        {
            return null;
        }

        if (!sourceCode.Any())
        {
            return null;
        }

        var result = await CompileNewAssemblyAsync(efDbConnection, sourceCode);

        if (!result.Success)
        {
            throw new Exception("Could not compile data connection assembly. " +
                                $"Compilation failed with the following diagnostics: \n{string.Join("\n", result.Diagnostics)}");
        }

        return result.AssemblyBytes;
    }

    public async Task<Reference[]> GetRequiredReferencesAsync(DataConnection dataConnection)
    {
        if (dataConnection is not EntityFrameworkDatabaseConnection efConnection)
            return Array.Empty<Reference>();

        var references = new List<Reference>();

        // references.Add(new PackageReference(
        //     "Microsoft.EntityFrameworkCore",
        //     "Entity Framework Core",
        //     EntityFrameworkPackageUtil.GetEntityFrameworkCoreVersion()));

        references.Add(new PackageReference(
            efConnection.EntityFrameworkProviderName,
            efConnection.EntityFrameworkProviderName,
            await EntityFrameworkPackageUtil.GetEntityFrameworkProviderVersionAsync(_packageProvider, efConnection.EntityFrameworkProviderName)
                ?? throw new Exception($"Could not find a version for entity framework provider: '{efConnection.EntityFrameworkProviderName}'")
        ));

        return references.ToArray();
    }

    private async Task<CompilationResult> CompileNewAssemblyAsync(EntityFrameworkDatabaseConnection efConnection, SourceCodeCollection sourceCode)
    {
        var references = new HashSet<string>();

        var requiredReferences = await GetRequiredReferencesAsync(efConnection);
        references.AddRange(await requiredReferences.GetAssemblyPathsAsync(_packageProvider));

        var code = sourceCode.GetText();

        return _codeCompiler.Compile(new CompilationInput(
                code,
                references)
            .WithOutputKind(OutputKind.DynamicallyLinkedLibrary)
            .WithOutputAssemblyNameTag($"data-connection_{efConnection.Id}")
        );
    }

    private SourceCode GenerateUtilityCode(ScaffoldedDatabaseModel model)
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

        var utilCode = new StringBuilder();

        // 1. Make the Program class inherit the generated DbContext
        var dbContext = model.DbContextFile;
        utilCode.AppendLine($"public partial class Program : {dbContext.ClassName}")
            .AppendLine("{");

        // 2. Add the DbContext property
        utilCode
            .AppendLine("\tprivate static Program? _program;")
            .AppendLine(@"
    /// <summary>
    /// The DbContext instance used to access the database.
    /// </summary>
    public static Program DataContext => _program ??= new Program();");


        var dbContextCodeLines = dbContext.Code!.Split(Environment.NewLine).ToList();

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
            var dbContextPropertyName = $"{propertyName}DbSet";

            programProperties.Add($@"
    /// <summary>
    /// The {propertyName} table (DbSet).
    /// </summary>
    private static System.Linq.IQueryable<{entityType}> {propertyName} => DataContext.{dbContextPropertyName};");

            // Rename property on DbContext
            dbContextCodeLines[iLine] = line.Replace(propertyName, dbContextPropertyName);
        }

        // Replace the DbContext code since we modified it above when renaming properties
        dbContext.Code = dbContextCodeLines.JoinToString(Environment.NewLine);

        utilCode.AppendJoin(Environment.NewLine, programProperties)
            .AppendLine()
            .AppendLine("}");

        // Add a new code file for this utility class
        return new SourceCode(utilCode.ToString());
    }
}
