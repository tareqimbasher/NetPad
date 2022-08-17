using NetPad.Configuration;
using NetPad.DotNet;
using NetPad.IO;
using NetPad.Scripts;

namespace NetPad.Plugins.OmniSharp;

public class ScriptProject : DotNetCSharpProject
{
    private readonly ILogger<ScriptProject> _logger;

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
        Script = script;

        BootstrapperProgramFilePath = Path.Combine(ProjectDirectoryPath, "Bootstrapper_Program.cs");
        UserProgramFilePath = Path.Combine(ProjectDirectoryPath, "User_Program.cs");
    }

    public Script Script { get; }
    public string BootstrapperProgramFilePath { get; }
    public string UserProgramFilePath { get; }

    public override async Task CreateAsync(bool deleteExisting = false)
    {
        await base.CreateAsync(deleteExisting);

        var domainAssembly = typeof(IOutputWriter).Assembly;
        await AddAssemblyReferenceAsync(domainAssembly.Location);

        foreach (var reference in Script.Config.References)
        {
            if (reference is PackageReference pkgRef)
            {
                await AddPackageAsync(pkgRef.PackageId, pkgRef.Version);
            }
            else if (reference is AssemblyReference asmRef)
            {
                await AddAssemblyReferenceAsync(asmRef.AssemblyPath);
            }
        }
    }
}
