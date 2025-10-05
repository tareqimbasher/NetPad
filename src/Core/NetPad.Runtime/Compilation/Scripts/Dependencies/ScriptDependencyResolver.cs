using NetPad.Data;
using NetPad.Data.Metadata;
using NetPad.DotNet;
using NetPad.DotNet.References;
using NetPad.Packages;
using NetPad.Scripts;

namespace NetPad.Compilation.Scripts.Dependencies;

public class ScriptDependencyResolver(
    IDataConnectionResourcesCache dataConnectionResourcesCache,
    IPackageProvider packageProvider) : IScriptDependencyResolver
{
    /// <summary>
    /// Gets all dependencies a script needs to run.
    /// </summary>
    public async Task<ScriptDependencies> GetDependenciesAsync(Script script, CancellationToken cancellationToken)
    {
        var references = new List<ReferenceDependency>();
        var code = new List<CodeDependency>();

        // Add script references
        references.AddRange(script.Config.References
            .Select(x => new ReferenceDependency(x, Dependant.Script, DependencyLoadStrategy.LoadInPlace)));

        if (cancellationToken.IsCancellationRequested)
        {
            return new(references, code);
        }

        // Add data connection resources
        if (script.DataConnection != null)
        {
            await AddDataConnectionResourcesAsync(
                script.DataConnection,
                script.Config.TargetFrameworkVersion,
                references,
                code,
                cancellationToken);
        }

        // Add built-in assemblies needed to run script
        references.AddRange(((IScriptDependencyResolver)this).GetUserVisibleAssemblies()
            .Select(assemblyPath => new ReferenceDependency(
                new AssemblyFileReference(assemblyPath),
                Dependant.Shared,
                DependencyLoadStrategy.DeployAndLoad))
        );

        if (cancellationToken.IsCancellationRequested)
        {
            return new(references, code);
        }

        Task.WaitAll(references
                .Select(d => d.LoadAssetsAsync(
                    script.Config.TargetFrameworkVersion,
                    packageProvider,
                    cancellationToken))
                .ToArray(),
            cancellationToken
        );

        return new(references, code);
    }

    private async Task AddDataConnectionResourcesAsync(
        DataConnection dataConnection,
        DotNetFrameworkVersion targetFrameworkVersion,
        List<ReferenceDependency> references,
        List<CodeDependency> code,
        CancellationToken cancellationToken)
    {
        var connectionResources = await dataConnectionResourcesCache.GetResourcesAsync(
            dataConnection,
            targetFrameworkVersion,
            cancellationToken);

        var applicationCode = connectionResources.SourceCode?.ApplicationCode;
        if (applicationCode?.Count > 0)
        {
            code.Add(new CodeDependency(applicationCode));
        }

        var requiredReferences = connectionResources.RequiredReferences;
        if (requiredReferences?.Length > 0)
        {
            references.AddRange(requiredReferences
                .Select(x => new ReferenceDependency(x, Dependant.Shared, DependencyLoadStrategy.DeployAndLoad)));
        }

        if (connectionResources.Assembly != null)
        {
            references.Add(new ReferenceDependency(
                new AssemblyImageReference(connectionResources.Assembly),
                Dependant.Shared,
                DependencyLoadStrategy.DeployAndLoad));
        }
    }
}
