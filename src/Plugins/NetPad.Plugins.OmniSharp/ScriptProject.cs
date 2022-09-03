using NetPad.Configuration;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.IO;
using NetPad.Scripts;

namespace NetPad.Plugins.OmniSharp;

public class ScriptProject : DotNetCSharpProject
{
    private readonly ILogger<ScriptProject> _logger;
    private readonly HashSet<Reference> _existingDataConnectionReferences;
    private readonly SemaphoreSlim _dataConnectionReferencesLock;

    public ScriptProject(
        Script script,
        Settings settings,
        ILogger<ScriptProject> logger)
        : base(
            Path.Combine(Path.GetTempPath(), "NetPad", script.Id.ToString()),
            "script.csproj",
            Path.Combine(settings.PackageCacheDirectoryPath, "NuGet")
        )
    {
        _logger = logger;
        _existingDataConnectionReferences = new HashSet<Reference>();
        _dataConnectionReferencesLock = new SemaphoreSlim(1, 1);

        Script = script;
        BootstrapperProgramFilePath = Path.Combine(ProjectDirectoryPath, "Bootstrapper_Program.cs");
        UserProgramFilePath = Path.Combine(ProjectDirectoryPath, "User_Program.cs");
        DataConnectionProgramFilePath = Path.Combine(ProjectDirectoryPath, "Data_Connection_Program.cs");
    }

    public Script Script { get; }
    public string BootstrapperProgramFilePath { get; }
    public string UserProgramFilePath { get; }
    public string DataConnectionProgramFilePath { get; }

    public override async Task CreateAsync(ProjectOutputType outputType, bool deleteExisting = false)
    {
        await base.CreateAsync(outputType, deleteExisting);

        var domainAssembly = typeof(IOutputWriter).Assembly;
        await AddAssemblyReferenceAsync(new AssemblyReference(domainAssembly.Location));

        foreach (var reference in Script.Config.References)
        {
            if (reference is PackageReference packageReference)
            {
                await AddPackageAsync(packageReference);
            }
            else if (reference is AssemblyReference assemblyReference)
            {
                await AddAssemblyReferenceAsync(assemblyReference);
            }
        }
    }

    public override Task AddAssemblyReferenceAsync(AssemblyReference reference)
    {
        try
        {
            return base.AddAssemblyReferenceAsync(reference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add assembly reference to project. " +
                                 "Assembly path: {AssemblyPath}", reference.AssemblyPath);

            return Task.FromException(ex);
        }
    }

    public override Task RemoveAssemblyReferenceAsync(AssemblyReference reference)
    {
        try
        {
            return base.RemoveAssemblyReferenceAsync(reference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove assembly reference from project. " +
                                 "Assembly path: {AssemblyPath}", reference.AssemblyPath);

            return Task.FromException(ex);
        }
    }

    public override Task AddPackageAsync(PackageReference reference)
    {
        try
        {
            return base.AddPackageAsync(reference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add package to project. " +
                                 "Package ID: {PackageId}. Package version: {PackageVersion}",
                reference.PackageId,
                reference.Version);

            return Task.FromException(ex);
        }
    }

    public override Task RemovePackageAsync(PackageReference reference)
    {
        try
        {
            return base.RemovePackageAsync(reference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove package from project. " +
                                 "Package ID: {PackageId}. Package version: {PackageVersion}",
                reference.PackageId,
                reference.Version);

            return Task.FromException(ex);
        }
    }

    public async Task UpdateReferencesFromDataConnectionAsync(DataConnection? dataConnection, IDataConnectionResourcesCache dataConnectionResourcesCache)
    {
        if (dataConnection == null && !_existingDataConnectionReferences.Any())
        {
            return;
        }

        await _dataConnectionReferencesLock.WaitAsync();

        try
        {
            if (dataConnection == null)
            {
                foreach (var reference in _existingDataConnectionReferences)
                {
                    await RemoveDataConnectionReferenceAsync(reference);
                }

                return;
            }

            var dataConnectionReferences = await dataConnectionResourcesCache.GetRequiredReferencesAsync(dataConnection);

            if (dataConnectionReferences.All(_existingDataConnectionReferences.Contains) &&
                _existingDataConnectionReferences.All(dataConnectionReferences.Contains))
            {
                return;
            }

            // Remove the ones we no longer need
            var toRemove = _existingDataConnectionReferences.Except(dataConnectionReferences).ToArray();
            foreach (var reference in toRemove)
            {
                await RemoveDataConnectionReferenceAsync(reference);
            }

            // Add new ones
            var toAdd = dataConnectionReferences.Except(_existingDataConnectionReferences).ToArray();
            foreach (var reference in toAdd)
            {
                await AddDataConnectionReferenceAsync(reference);
            }
        }
        finally
        {
            _dataConnectionReferencesLock.Release();
        }
    }

    private async Task AddDataConnectionReferenceAsync(Reference reference)
    {
        try
        {
            await AddReferenceAsync(reference);
            _existingDataConnectionReferences.Add(reference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove data connection reference: {Reference}", reference.ToString());
        }
    }

    private async Task RemoveDataConnectionReferenceAsync(Reference reference)
    {
        try
        {
            await RemoveReferenceAsync(reference);
            _existingDataConnectionReferences.Remove(reference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove data connection reference: {Reference}", reference.ToString());
        }
    }
}
