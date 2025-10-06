using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using NetPad.Common;
using NetPad.Compilation;
using NetPad.Compilation.Scripts.Dependencies;
using NetPad.DotNet;
using NetPad.DotNet.References;
using NetPad.IO;
using NetPad.Presentation;

namespace NetPad.ExecutionModel.ClientServer;

public partial class ClientServerScriptRunner
{
    /// <summary>
    /// Contains post-setup info needed to run the script.
    /// </summary>
    /// <param name="ScriptDir">The directory that will be used to deploy the script
    /// assembly and any dependencies that are only needed by the script.</param>
    /// <param name="ScriptAssemblyFilePath">The full path to the compiled script assembly.</param>
    /// <param name="InPlaceDependencyDirectories">Directories where dependencies where not copied to one of the
    /// deployment directories and instead should be loaded from their original locations (in-place).</param>
    /// <param name="UserProgramStartLineNumber">The line number that user code starts on.</param>
    private record SetupInfo(
        DirectoryPath ScriptDir,
        FilePath ScriptAssemblyFilePath,
        string[] InPlaceDependencyDirectories,
        int UserProgramStartLineNumber);

    /// <summary>
    /// Sets up the environment the script will run in. It creates a folder structure similar to the following and
    /// deploys dependencies needed to run the script-host process and the script itself to these folders:
    /// <code>
    /// /root                   # A temp directory created for each script when the script is run
    ///     /script-host        # Contains the script-host executable
    ///     /shared-deps        # Contains all dependency assets (assemblies, binaries...etc) that are specific
    ///                           to the script-host process or are shared between script-host and the script assembly
    ///     /script             # Contains a sub-directory for each instance the user runs the script. Each sub dir
    ///                           contains the script assembly and all dependency assets that only the script assembly needs to run
    /// </code>
    /// <para>
    /// Some dependencies are copied (deployed) to this folder structure where they will be loaded from, while other
    /// dependencies will not be copied, and instead will be loaded from their original locations (in-place).
    /// </para>
    /// </summary>
    private async Task<SetupInfo?> SetupRunEnvironmentAsync(RunOptions runOptions, CancellationToken cancellationToken)
    {
        _workingDirectory.CreateIfNotExists();

        // Resolve and collect all dependencies needed by script-host and script
        _ = _appStatusMessagePublisher.PublishAsync(_script.Id, "Gathering dependencies...");
        var dependencies = await _scriptDependencyResolver.GetDependenciesAsync(_script, cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            return null;
        }

        // Parse and compile the script
        _ = _appStatusMessagePublisher.PublishAsync(_script.Id, "Compiling...");
        var parseCompileResult = await _scriptCompiler.ParseAndCompileAsync(
            runOptions.SpecificCodeToRun ?? _script.Code,
            _script,
            cancellationToken);

        if (parseCompileResult == null || cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("Parse and compile failed");
            return null;
        }

        var (parsingResult, compilationResult) = parseCompileResult;
        if (!compilationResult.Success)
        {
            var errors = compilationResult
                .Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => DiagnosicsHelper.ReduceStacktraceLineNumbers(d, parsingResult.UserProgramStartLineNumber));

            await _combinedOutputWriter.WriteAsync(
                new ErrorScriptOutput("Compilation failed:\n" + errors.JoinToString("\n")),
                cancellationToken: cancellationToken);

            return null;
        }

        // Deploy compiled script, the script-host, and their dependencies
        _ = _appStatusMessagePublisher.PublishAsync(_script.Id, "Preparing...");
        if (!_workingDirectory.ScriptHostExecutableFile.Exists())
        {
            DeployScriptHostExecutable(_workingDirectory);
        }

        await DeploySharedDependenciesAsync(_workingDirectory, dependencies);
        var (scriptDir, scriptAssemblyFilePath) = await DeployScriptDependenciesAsync(
            compilationResult.AssemblyBytes,
            dependencies);

        string[] inPlaceDependencyDirectories = dependencies.References
            .Where(x => x.LoadStrategy == DependencyLoadStrategy.LoadInPlace)
            .SelectMany(x => x.Assets.Select(a => Path.GetDirectoryName(a.Path)))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet()
            .ToArray()!;

        return new SetupInfo(
            scriptDir,
            scriptAssemblyFilePath,
            inPlaceDependencyDirectories,
            parsingResult.UserProgramStartLineNumber
        );
    }

    private static void DeployScriptHostExecutable(WorkingDirectory workingDirectory)
    {
        if (!workingDirectory.ScriptHostExecutableSourceDirectory.Exists())
        {
            throw new InvalidOperationException(
                $"Could not find source script-host executable directory to deploy. Path does not exist: {workingDirectory.ScriptHostExecutableSourceDirectory}");
        }

        // Copy script-host app to working dir
        FileSystemUtil.CopyDirectory(
            workingDirectory.ScriptHostExecutableSourceDirectory.Path,
            workingDirectory.ScriptHostExecutableRunDirectory.Path,
            true);
    }

    private static async Task DeploySharedDependenciesAsync(
        WorkingDirectory workingDirectory,
        ScriptDependencies dependencies)
    {
        workingDirectory.SharedDependenciesDirectory.CreateIfNotExists();

        var sharedDeps = dependencies.References
            .Where(x => x.Dependant is Dependant.ScriptHost or Dependant.Shared);

        await DeployAsync(workingDirectory.SharedDependenciesDirectory, sharedDeps);
    }

    private async Task<(DirectoryPath, FilePath)> DeployScriptDependenciesAsync(
        byte[] scriptAssembly,
        ScriptDependencies dependencies)
    {
        var scriptDeployDir = _workingDirectory.CreateNewScriptDeployDirectory();
        scriptDeployDir.CreateIfNotExists();

        var scriptDeps = dependencies.References.Where(x => x.Dependant is Dependant.Script).ToArray();

        await DeployAsync(scriptDeployDir, scriptDeps);

        // Write compiled assembly to dir
        var fileSafeScriptName = StringUtil
                                     .RemoveInvalidFileNameCharacters(_script.Name, "_")
                                     .Replace(" ", "_")
                                 // Arbitrary suffix so we don't match an assembly/asset with the same name.
                                 // Example: Assume user names script "Microsoft.Extensions.DependencyInjection"
                                 // If user also has a reference to "Microsoft.Extensions.DependencyInjection.dll"
                                 // then code further below will not copy the "Microsoft.Extensions.DependencyInjection.dll"
                                 // to the output directory, resulting in the referenced assembly not being found.
                                 + "__";

        var scriptAssemblyFilePath = scriptDeployDir.CombineFilePath($"{fileSafeScriptName}.dll");

        await File.WriteAllBytesAsync(scriptAssemblyFilePath.Path, scriptAssembly);

        // A runtimeconfig.json file tells .NET how to run the assembly
        var probingPaths = new[]
            {
                scriptDeployDir.Path,
                _workingDirectory.SharedDependenciesDirectory.Path,
            }
            .Union(scriptDeps.SelectMany(x => x.Assets.Select(a => Path.GetDirectoryName(a.Path)!)))
            .Where(x => !string.IsNullOrEmpty(x))
            .ToArray();

        await File.WriteAllTextAsync(
            Path.Combine(scriptDeployDir.Path, $"{fileSafeScriptName}.runtimeconfig.json"),
            GenerateRuntimeConfigFileContents(probingPaths)
        );

        // The scriptconfig.json is custom and passes some options to the running script
        await File.WriteAllTextAsync(
            Path.Combine(scriptDeployDir.Path, "scriptconfig.json"),
            $$"""
              {
                  "output": {
                      "maxDepth": {{_settings.Results.MaxSerializationDepth}},
                      "maxCollectionSerializeLength": {{_settings.Results.MaxCollectionSerializeLength}}
                  }
              }
              """);

        return (scriptDeployDir, scriptAssemblyFilePath);
    }

    private static async Task DeployAsync(DirectoryPath destination,
        IEnumerable<ReferenceDependency> referenceDependencies)
    {
        foreach (var dependency in referenceDependencies)
        {
            var reference = dependency.Reference;

            if (reference is AssemblyImageReference air)
            {
                var assemblyImage = air.AssemblyImage;
                var fileName = assemblyImage.ConstructAssemblyFileName();
                var destFilePath = destination.CombineFilePath(fileName);

                // Checking file exists means that the first assembly in the list of paths will win.
                // Later assemblies with the same file name will not be copied to the output directory.
                if (!destFilePath.Exists())
                {
                    await File.WriteAllBytesAsync(
                        destFilePath.Path,
                        assemblyImage.Image);
                }
            }

            if (dependency.LoadStrategy == DependencyLoadStrategy.LoadInPlace)
            {
                continue;
            }

            foreach (var asset in dependency.Assets)
            {
                var destFilePath = destination.CombineFilePath(Path.GetFileName(asset.Path));
                if (!destFilePath.Exists())
                {
                    File.Copy(asset.Path, destFilePath.Path, true);
                }
            }
        }
    }

    private string GenerateRuntimeConfigFileContents(string[] probingPaths)
    {
        var tfm = _script.Config.TargetFrameworkVersion.GetTargetFrameworkMoniker();
        var frameworkName = _script.Config.UseAspNet ? "Microsoft.AspNetCore.App" : "Microsoft.NETCore.App";
        int majorVersion = _script.Config.TargetFrameworkVersion.GetMajorVersion();

        var runtimeVersion = _dotNetInfo.GetDotNetRuntimeVersionsOrThrow()
            .Where(v => v.Version.Major == majorVersion &&
                        v.FrameworkName.Equals(frameworkName, StringComparison.OrdinalIgnoreCase))
            .MaxBy(v => v.Version)?
            .Version;

        if (runtimeVersion == null)
        {
            throw new Exception(
                $"Could not find a {tfm} runtime with the name {frameworkName} and version {majorVersion}");
        }

        var probingPathsJson = JsonSerializer.Serialize(probingPaths);

        return $$"""
                 {
                     "runtimeOptions": {
                         "tfm": "{{tfm}}",
                         "rollForward": "Minor",
                         "framework": {
                             "name": "{{frameworkName}}",
                             "version": "{{runtimeVersion}}"
                         },
                         "additionalProbingPaths": {{probingPathsJson}}
                     }
                 }
                 """;
    }
}
