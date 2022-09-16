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
using NetPad.Events;
using NetPad.Packages;
using NetPad.Utilities;

namespace NetPad.Data;

public class DataConnectionResourcesGenerator : IDataConnectionResourcesGenerator
{
    private readonly Settings _settings;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ICodeCompiler _codeCompiler;
    private readonly IPackageProvider _packageProvider;
    private readonly IEventBus _eventBus;

    public DataConnectionResourcesGenerator(
        ICodeCompiler codeCompiler,
        IPackageProvider packageProvider,
        IEventBus eventBus,
        Settings settings,
        ILoggerFactory loggerFactory)
    {
        _codeCompiler = codeCompiler;
        _packageProvider = packageProvider;
        _eventBus = eventBus;
        _settings = settings;
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    ///
    /// </summary>
    public string Type { get; set; }

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

        var utilCode = new StringBuilder();

        utilCode.AppendLine("public partial class Program")
            .AppendLine()
            .AppendLine("{")
            .AppendLine(@"
/// <summary>
/// An instantiated database context that you can use to access the database.
/// </summary>
public static DatabaseContext DataContext { get; } = new DatabaseContext();");

        var dbContext = model.SourceFiles.Single(f => f.IsDbContext);

        var dbSetProperties = dbContext.Code!.Split(Environment.NewLine)
            .Where(l => l.Contains("public virtual DbSet<"))
            .Select(l =>
            {
                // Extracts 'DbSet<Book> Books' from 'public virtual DbSet<Book> Books { get; set; } = null!;'
                var typeAndName = l.SubstringBetween("virtual ", " {");
                var parts = typeAndName.Split(" ");

                return new
                {
                    Type = parts[0],
                    Name = parts[1]
                };
            })
            .Select(dbSet => $@"
/// <summary>
/// The {dbSet.Name} table (DbSet).
/// </summary>
private static {dbSet.Type} {dbSet.Name} => DataContext.{dbSet.Name};");

        utilCode.AppendJoin(Environment.NewLine, dbSetProperties)
            .AppendLine()
            .AppendLine("}");

        code.Add(new SourceCode(utilCode.ToString()));

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
        var references = new List<string>();

        var latestVersion = await EntityFrameworkPackageUtil.GetEntityFrameworkProviderVersionAsync(_packageProvider, efConnection.EntityFrameworkProviderName);

        if (latestVersion == null)
        {
            throw new Exception($"Could not find a package version to install for Entity Framework provider: {efConnection.EntityFrameworkProviderName}.");
        }

        var providerAssemblies = await _packageProvider.GetPackageAndDependanciesAssembliesAsync(
            efConnection.EntityFrameworkProviderName,
            latestVersion);

        references.AddRange(providerAssemblies);

        var code = sourceCode.GetText();

        return _codeCompiler.Compile(new CompilationInput(
                code,
                references.ToHashSet())
            .WithOutputKind(OutputKind.DynamicallyLinkedLibrary)
            .WithOutputAssemblyNameTag($"data-connection_{efConnection.Id}")
        );
    }
}
