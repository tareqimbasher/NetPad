using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using NetPad.Common;
using NetPad.Compilation;
using NetPad.Configuration;
using NetPad.Data.Scaffolding;
using NetPad.DotNet;
using NetPad.Events;
using NetPad.Packages;
using NetPad.Scripts;

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

        var success = await scaffolder.ScaffoldAsync();

        if (!success)
        {
            throw new Exception("Database connection could not be scaffolded.");
        }

        var model = await scaffolder.GetScaffoldedModelAsync();

        return new SourceCodeCollection(model.SourceFiles);
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
                                $"Compilation failed with the following diagnostics: {string.Join("\n", result.Diagnostics)}");
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

        var code = sourceCode.ToParsedSourceCode();

        return _codeCompiler.Compile(new CompilationInput(
                code,
                references.ToHashSet())
            .WithOutputKind(OutputKind.DynamicallyLinkedLibrary));
    }
}
