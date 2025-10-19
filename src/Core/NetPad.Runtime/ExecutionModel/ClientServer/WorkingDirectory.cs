using NetPad.Configuration;
using NetPad.IO;

namespace NetPad.ExecutionModel.ClientServer;

/// <summary>
/// The root working directory used to deploy resources needed to run a script.
/// </summary>
public record WorkingDirectory : DirectoryPath
{
    public WorkingDirectory(Guid scriptId) : base(AppDataProvider.ClientServerProcessesDirectoryPath.Combine(scriptId.ToString()))
    {
        ScriptHostExecutableSourceDirectory = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(typeof(ClientServerScriptRunner).Assembly.Location) ?? string.Empty,
            "ScriptHost"
        );

        ScriptHostExecutableRunDirectory = Combine("script-host");
        var executableName = "netpad-script-host" + PlatformUtil.GetPlatformExecutableExtension();
        ScriptHostExecutableFile = ScriptHostExecutableRunDirectory.CombineFilePath(executableName);

        DependenciesDirectory = Combine("script-host-deps");
        SharedDependenciesDirectory = Combine("shared-deps");
        ScriptDeployDirectoryRoot = Combine("script");
    }

    /// <summary>
    /// The directory that is part of the installed NetPad package that contains the script-host process executable.
    /// </summary>
    public DirectoryPath ScriptHostExecutableSourceDirectory { get; }

    /// <summary>
    /// The directory where the script-host executable is deployed to and run from.
    /// </summary>
    public DirectoryPath ScriptHostExecutableRunDirectory { get; }

    /// <summary>
    /// The file path to the deployed script-host executable.
    /// </summary>
    public FilePath ScriptHostExecutableFile { get; }

    /// <summary>
    /// The directory where dependencies needed by the running script are deployed to.
    /// </summary>
    public DirectoryPath DependenciesDirectory { get; }

    /// <summary>
    /// The directory that is used to deploy script-host dependencies as well as dependencies needed by both
    /// the script-host process and the script itself.
    /// </summary>
    public DirectoryPath SharedDependenciesDirectory { get; }

    /// <summary>
    /// The directory where compiled script and its related assets are deployed to.
    /// </summary>
    public DirectoryPath ScriptDeployDirectoryRoot { get; }

    /// <summary>
    /// Creates a new directory that will be used to deploy a script assembly and its dependencies.
    /// </summary>
    public DirectoryPath CreateNewScriptDeployDirectory()
    {
        return ScriptDeployDirectoryRoot.Combine(DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss-fff"));
    }
}
