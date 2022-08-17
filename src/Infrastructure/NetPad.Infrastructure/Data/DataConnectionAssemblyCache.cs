using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using NetPad.Compilation;
using NetPad.Packages;

namespace NetPad.Data;

public class DataConnectionAssemblyCache : IDataConnectionAssemblyCache
{
    private readonly IDataConnectionSourceCodeCache _dataConnectionSourceCodeCache;
    private readonly ICodeCompiler _codeCompiler;
    private readonly IPackageProvider _packageProvider;
    private readonly Dictionary<Guid, byte[]> _assemblies;

    public DataConnectionAssemblyCache(
        IDataConnectionSourceCodeCache dataConnectionSourceCodeCache,
        ICodeCompiler codeCompiler,
        IPackageProvider packageProvider)
    {
        _dataConnectionSourceCodeCache = dataConnectionSourceCodeCache;
        _codeCompiler = codeCompiler;
        _packageProvider = packageProvider;
        _assemblies = new Dictionary<Guid, byte[]>();
    }

    public async Task<byte[]?> GetAssemblyAsync(DataConnection dataConnection)
    {
        if (dataConnection is not EntityFrameworkDatabaseConnection efConnection)
        {
            return null;
        }

        if (_assemblies.TryGetValue(efConnection.Id, out var assemblyBytes))
        {
            return assemblyBytes;
        }

        var codeCollection = await _dataConnectionSourceCodeCache.GetSourceGeneratedCodeAsync(efConnection);
        if (!codeCollection.Any())
        {
            return null;
        }

        var result = await CompileNewAssemblyAsync(efConnection, codeCollection);

        if (!result.Success)
        {
            throw new Exception("Could not compile data connection assembly. " +
                                $"Compilation failed with the following diagnostics: {string.Join("\n", result.Diagnostics)}");
        }

        _assemblies.Add(efConnection.Id, result.AssemblyBytes);
        return result.AssemblyBytes;
    }

    private async Task<CompilationResult> CompileNewAssemblyAsync(EntityFrameworkDatabaseConnection efConnection, SourceCodeCollection codeCollection)
    {
        var references = new List<string>();

        var search = await _packageProvider.SearchPackagesAsync(efConnection.EntityFrameworkProviderName, 0, 5, false, false);
        var package = search.FirstOrDefault(p => p.PackageId == efConnection.EntityFrameworkProviderName);

        if (package == null)
        {
            throw new Exception($"Could not find a package to install for Entity Framework provider: {efConnection.EntityFrameworkProviderName}.");
        }

        var providerAssemblies = await _packageProvider.GetPackageAndDependanciesAssembliesAsync(package.PackageId, package.Version!);

        references.AddRange(providerAssemblies);

        var code = codeCollection.ToParsedSourceCode();

        return _codeCompiler.Compile(new CompilationInput(
                code,
                references.ToHashSet())
            .WithOutputKind(OutputKind.DynamicallyLinkedLibrary));
    }
}
