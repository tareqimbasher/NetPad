using NetPad.Configuration;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.Scripts;

namespace NetPad.Plugins.OmniSharp;

public class OmniSharpProject : DotNetCSharpProject
{
    private readonly ILogger<OmniSharpProject> _logger;
    private readonly HashSet<Reference> _existingDataConnectionReferences;
    private readonly SemaphoreSlim _dataConnectionReferencesLock;

    public OmniSharpProject(
        Script script,
        IDotNetInfo dotNetInfo,
        Settings settings,
        ILogger<OmniSharpProject> logger)
        : base(
            dotNetInfo,
            Consts.OmniSharpServerProcessesDirectoryPath.Combine(
                script.Id.ToString()).Path,
            "script.csproj",
            Path.Combine(settings.PackageCacheDirectoryPath, "NuGet")
        )
    {
        _logger = logger;
        _existingDataConnectionReferences = [];
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

    public override async Task CreateAsync(
        DotNetFrameworkVersion targetDotNetFrameworkVersion,
        ProjectOutputType outputType,
        DotNetSdkPack sdkPack = DotNetSdkPack.NetApp,
        bool enableNullable = true,
        bool enableImplicitUsings = true)
    {
        await base.CreateAsync(targetDotNetFrameworkVersion, outputType, sdkPack, enableNullable, enableImplicitUsings);
        await AddReferencesAsync(Script.Config.References);
    }

    public async Task UpdateReferencesFromDataConnectionAsync(DataConnection? dataConnection, IEnumerable<Reference> dataConnectionReferences)
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating project references from data connection");
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
            _logger.LogError(ex, "Failed to add data connection reference: {Reference}", reference.ToString());
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
