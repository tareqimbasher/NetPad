using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using NetPad.Common;
using NetPad.Compilation;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.ExecutionModel.External;
using NetPad.IO;
using NetPad.Packages;
using NetPad.Presentation;

namespace NetPad.ExecutionModel.ClientServer;

public partial class ClientServerScriptRunner
{
    private readonly ICodeParser _codeParser;
    private readonly ICodeCompiler _codeCompiler;
    private readonly IPackageProvider _packageProvider;
    private readonly Settings _settings;

    private record SetupInfo(
        DirectoryPath ScriptHostDepsDir,
        DirectoryPath ScriptDir,
        FilePath ScriptAssemblyFilePath,
        int UserProgramStartLineNumber);

    /// <summary>
    /// Sets up the environment the script will run in. It sets up a folder structure similar to the following:
    /// <code>
    /// /script-root            # A temp directory created for each script when the script is run
    ///     /script-host        # Contains all dependency assets (assemblies, binaries...etc) that are specific
    ///                           to the script-host process or are shared between script-host and the script assembly.
    ///     /script-run-1       # A new dir is created for each script run. It contains the script assembly and all
    ///                           dependency assets that only the script assembly needs to run
    ///     /script-run-2
    ///     /script-run-3
    ///     /...
    /// </code>
    /// </summary>
    private async Task<SetupInfo?> SetupRunEnvironment(RunOptions runOptions)
    {
        var dependencies = new List<Dependency>();
        var additionalCode = new SourceCodeCollection();

        // Add script references
        dependencies.AddRange(_script.Config.References
            .Select(x => new Dependency(x, ReferenceNeededBy.Script)));

        // Add data connection resources
        if (_script.DataConnection != null)
        {
            var dcResources = await GetDataConnectionResourcesAsync(_script.DataConnection);

            if (dcResources.Code.Count > 0)
            {
                additionalCode.AddRange(dcResources.Code);
            }

            if (dcResources.References.Count > 0)
            {
                dependencies.AddRange(dcResources.References
                    .Select(x => new Dependency(x, ReferenceNeededBy.Shared)));
            }

            if (dcResources.Assembly != null)
            {
                dependencies.Add(
                    new Dependency(new AssemblyImageReference(dcResources.Assembly), ReferenceNeededBy.Shared));
            }
        }

        // Add assembly files needed to support running script
        dependencies.AddRange(_userVisibleAssemblies
            .Select(assemblyPath => new Dependency(new AssemblyFileReference(assemblyPath), ReferenceNeededBy.Shared))
        );

        Task.WaitAll(dependencies
            .Select(d => d.LoadAssetsAsync(_script.Config.TargetFrameworkVersion, _packageProvider))
            .ToArray()
        );

        var compileAssemblyImageDeps = dependencies
            .Select(d =>
                d.NeededBy != ReferenceNeededBy.ScriptHost && d.Reference is AssemblyImageReference air
                    ? air.AssemblyImage
                    : null!)
            .Where(x => x != null!)
            .ToArray();

        var compileAssemblyFileDeps = dependencies
            .Where(x => x.NeededBy != ReferenceNeededBy.ScriptHost)
            .SelectMany(x => x.Assets)
            .DistinctBy(x => x.Path)
            .Where(x => x.IsManagedAssembly)
            .Select(x => new
            {
                x.Path,
                AssemblyName = AssemblyName.GetAssemblyName(x.Path)
            })
            // Choose the highest version of duplicate assemblies
            .GroupBy(a => a.AssemblyName.Name)
            .Select(grp => grp.OrderBy(x => x.AssemblyName.Version).Last())
            .Select(x => x.Path)
            .ToHashSet();

        // Parse Code & Compile
        var (parsingResult, compilationResult) = ParseAndCompile.Do(
            runOptions.SpecificCodeToRun ?? _script.Code,
            _script,
            _codeParser,
            _codeCompiler,
            compileAssemblyImageDeps,
            compileAssemblyFileDeps,
            additionalCode);

        if (!compilationResult.Success)
        {
            var errors = compilationResult
                .Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => CorrectDiagnosticErrorLineNumber(d, parsingResult.UserProgramStartLineNumber));

            await _output.WriteAsync(new ErrorScriptOutput("Compilation failed:\n" + errors.JoinToString("\n")));

            return null;
        }

        var scriptHostDepsDir = await DeployScriptHostDependenciesAsync(dependencies);

        var (scriptDir, scriptAssemblyFilePath) = await DeployScriptDependenciesAsync(
            compilationResult.AssemblyBytes,
            scriptHostDepsDir,
            dependencies);

        return new SetupInfo(
            scriptHostDepsDir,
            scriptDir,
            scriptAssemblyFilePath,
            parsingResult.UserProgramStartLineNumber
        );
    }

    private async Task<(SourceCodeCollection Code, IReadOnlyList<Reference> References, AssemblyImage? Assembly)>
        GetDataConnectionResourcesAsync(DataConnection dataConnection)
    {
        var code = new SourceCodeCollection();
        var references = new List<Reference>();

        var targetFrameworkVersion = _script.Config.TargetFrameworkVersion;

        var connectionResources =
            await _dataConnectionResourcesCache.GetResourcesAsync(dataConnection, targetFrameworkVersion);

        var applicationCode = connectionResources.SourceCode?.ApplicationCode;
        if (applicationCode?.Count > 0)
        {
            code.AddRange(applicationCode);
        }

        var requiredReferences = connectionResources.RequiredReferences;
        if (requiredReferences?.Length > 0)
        {
            references.AddRange(requiredReferences);
        }

        return (code, references, connectionResources.Assembly);
    }

    private async Task<DirectoryPath> DeployScriptHostDependenciesAsync(IList<Dependency> dependencies)
    {
        var scriptHostDepsDirPath = _scriptHostRootDirectory.Combine("script-host");
        var scriptHostDepsDir = scriptHostDepsDirPath.GetInfo();

        scriptHostDepsDir.Create();

        var scriptHostDeps = dependencies
            .Where(x => x.NeededBy is ReferenceNeededBy.ScriptHost or ReferenceNeededBy.Shared);

        await DeployAsync(scriptHostDepsDir, scriptHostDeps);
        return scriptHostDepsDirPath;
    }

    private async Task<(DirectoryPath, FilePath)> DeployScriptDependenciesAsync(
        byte[] scriptAssembly,
        DirectoryPath scriptHostDepsDir,
        IList<Dependency> dependencies)
    {
        var scriptDirPath = Path.Combine(
            _scriptHostRootDirectory.Path,
            "script",
            DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss-fff"));

        var scriptDir = new DirectoryInfo(scriptDirPath);
        scriptDir.Create();

        var scriptDeps = dependencies.Where(x => x.NeededBy is ReferenceNeededBy.Script).ToArray();

        await DeployAsync(scriptDir, scriptDeps);

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

        FilePath scriptAssemblyFilePath = Path.Combine(scriptDir.FullName, $"{fileSafeScriptName}.dll");

        await File.WriteAllBytesAsync(scriptAssemblyFilePath.Path, scriptAssembly);

        // A runtimeconfig.json file tells .NET how to run the assembly
        var probingPaths = new[]
            {
                scriptDir.FullName,
                scriptHostDepsDir.Path,
            }
            .Concat(scriptDeps.SelectMany(x => x.Assets.Select(a => Path.GetDirectoryName(a.Path)!)))
            .Distinct()
            .Where(x => !string.IsNullOrEmpty(x))
            .ToArray();

        await File.WriteAllTextAsync(
            Path.Combine(scriptDir.FullName, $"{fileSafeScriptName}.runtimeconfig.json"),
            GenerateRuntimeConfigFileContents(probingPaths)
        );

        // The scriptconfig.json is custom and passes some options to the running script
        await File.WriteAllTextAsync(
            Path.Combine(scriptDir.FullName, "scriptconfig.json"),
            $@"{{
    ""output"": {{
        ""maxDepth"": {_settings.Results.MaxSerializationDepth},
        ""maxCollectionSerializeLength"": {_settings.Results.MaxCollectionSerializeLength}
    }}
}}");

        return (scriptDirPath, scriptAssemblyFilePath);
    }

    private async Task DeployAsync(DirectoryInfo destination, IEnumerable<Dependency> dependencies)
    {
        foreach (var dependency in dependencies)
        {
            var reference = dependency.Reference;

            if (reference is AssemblyImageReference air)
            {
                var assemblyImage = air.AssemblyImage;
                var fileName = assemblyImage.ConstructAssemblyFileName();
                var destFilePath = Path.Combine(destination.FullName, fileName);

                // Checking file exists means that the first assembly in the list of paths will win.
                // Later assemblies with the same file name will not be copied to the output directory.
                if (!File.Exists(destFilePath))
                {
                    await File.WriteAllBytesAsync(
                        destFilePath,
                        assemblyImage.Image);
                }
            }

            foreach (var asset in dependency.Assets)
            {
                var destFilePath = Path.Combine(destination.FullName, Path.GetFileName(asset.Path));
                if (!File.Exists(destFilePath))
                {
                    File.Copy(asset.Path, destFilePath, true);
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

    /// <summary>
    /// Corrects line numbers in compilation errors relative to the line number where user code starts.
    /// </summary>
    private static string CorrectDiagnosticErrorLineNumber(Diagnostic diagnostic, int userProgramStartLineNumber)
    {
        var err = diagnostic.ToString();

        if (!err.StartsWith('('))
        {
            return err;
        }

        var errParts = err.Split(':');
        var span = errParts.First().Trim(['(', ')']);
        var spanParts = span.Split(',');
        var lineNumberStr = spanParts[0];

        return int.TryParse(lineNumberStr, out int lineNumber)
            ? $"({lineNumber - userProgramStartLineNumber},{spanParts[1]}):{errParts.Skip(1).JoinToString(":")}"
            : err;
    }

    /// <summary>
    /// The component that a reference is needed by.
    /// </summary>
    enum ReferenceNeededBy
    {
        Script,
        ScriptHost,
        Shared,
    }

    /// <summary>
    /// A dependency needed by the run environment.
    /// </summary>
    /// <param name="Reference">A reference dependency.</param>
    /// <param name="NeededBy">Which components need this dependency to run.</param>
    record Dependency(Reference Reference, ReferenceNeededBy NeededBy)
    {
        public ReferenceAsset[] Assets { get; private set; } = [];

        public async Task LoadAssetsAsync(
            DotNetFrameworkVersion dotNetFrameworkVersion,
            IPackageProvider packageProvider)
        {
            Assets = (await Reference.GetAssetsAsync(dotNetFrameworkVersion, packageProvider)).ToArray();
        }
    }
}
