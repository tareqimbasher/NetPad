using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using NetPad.Common;
using NetPad.Compilation;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.DotNet.References;
using NetPad.IO;
using NetPad.Packages;
using NetPad.Presentation;
using O2Html;
using SourceCodeCollection = NetPad.DotNet.CodeAnalysis.SourceCodeCollection;

namespace NetPad.ExecutionModel.External;

public partial class ExternalScriptRunner
{
    private readonly ICodeParser _codeParser;
    private readonly ICodeCompiler _codeCompiler;
    private readonly IPackageProvider _packageProvider;
    private readonly Settings _settings;

    private async Task<DeploymentDirectory?> BuildAndDeployAsync(RunOptions runOptions, bool noCache, bool forceRebuild)
    {
        var deploymentDirectory = noCache
            ? _deploymentCache.CreateTempDeploymentDirectory()
            : _deploymentCache.GetOrCreateDeploymentDirectory(_script, forceRebuild);

        if (deploymentDirectory.ContainsDeployment)
        {
            return deploymentDirectory;
        }

        var deployDependencies = await BuildAsync(runOptions);
        if (deployDependencies == null)
        {
            return null;
        }

        var deploymentInfo = await DeployAsync(deploymentDirectory, deployDependencies);
        deploymentDirectory.SaveDeploymentInfo(deploymentInfo);
        return deploymentDirectory;
    }

    private async Task<DeployDependencies?> BuildAsync(RunOptions runOptions)
    {
        var references = new List<Reference>();
        var additionalCode = new SourceCodeCollection();

        // Add script references
        references.AddRange(_script.Config.References);

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
                references.AddRange(dcResources.References);
            }
        }

        // Resolve all assembly images
        var images = references
            .Select(r => r is AssemblyImageReference air ? air.AssemblyImage : null!)
            .Where(r => r != null!)
            .ToList();


        // Resolve all assembly assets
        var referenceAssets = (
                await references.GetAssetsAsync(_script.Config.TargetFrameworkVersion, _packageProvider)
            )
            .DistinctBy(a => a.Path)
            .ToArray();

        // Get assembly file assets
        var assemblyFilePaths = referenceAssets
            .Where(a => a.IsManagedAssembly)
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


        // Add assembly files needed to support running external process
        var supportAssemblies = _supportAssemblies.Concat([
            typeof(INetPadRuntimeLibMarker).Assembly.Location,
            typeof(HtmlSerializer).Assembly.Location
        ]);
        foreach (var assemblyPath in supportAssemblies)
        {
            assemblyFilePaths.Add(assemblyPath);
        }

        // Parse Code & Compile
        var (parsingResult, compilationResult) = ParseAndCompile.Do(
            runOptions.SpecificCodeToRun ?? _script.Code,
            _script,
            _codeParser,
            _codeCompiler,
            images,
            assemblyFilePaths,
            additionalCode);

        if (!compilationResult.Success)
        {
            var errors = compilationResult
                .Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => DiagnosicsHelper.ReduceStacktraceLineNumbers(d, parsingResult.UserProgramStartLineNumber));

            await _output.WriteAsync(new ErrorScriptOutput("Compilation failed:\n" + errors.JoinToString("\n")));

            return null;
        }

        // Get non-assembly file assets
        var fileAssets = referenceAssets
            .Where(a => !a.IsManagedAssembly)
            .Select(a => new FileAssetCopy(a.Path, $"./{Path.GetFileName(a.Path)}"))
            .ToHashSet();

        return new DeployDependencies(
            parsingResult,
            compilationResult.AssemblyBytes,
            images,
            assemblyFilePaths,
            fileAssets
        );
    }

    private async Task<(SourceCodeCollection Code, IReadOnlyList<Reference> References)>
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

        var connectionAssembly = connectionResources.Assembly;
        if (connectionAssembly != null)
        {
            references.Add(new AssemblyImageReference(connectionAssembly));
        }

        var requiredReferences = connectionResources.RequiredReferences;
        if (requiredReferences?.Length > 0)
        {
            references.AddRange(requiredReferences);
        }

        return (code, references);
    }

    private async Task<DeploymentInfo> DeployAsync(DeploymentDirectory deploymentDirectory,
        DeployDependencies deployDependencies)
    {
        var buildDirectoryPath = deploymentDirectory.Path;

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

        FilePath scriptAssemblyFilePath = deploymentDirectory.CombineFilePath($"{fileSafeScriptName}.dll");

        await File.WriteAllBytesAsync(scriptAssemblyFilePath.Path, deployDependencies.ScriptAssemblyBytes);

        // A runtimeconfig.json file tells .NET how to run the assembly
        await File.WriteAllTextAsync(
            Path.Combine(buildDirectoryPath, $"{fileSafeScriptName}.runtimeconfig.json"),
            GenerateRuntimeConfigFileContents(deployDependencies)
        );

        // The scriptconfig.json is custom and passes some options to the running script
        await File.WriteAllTextAsync(
            Path.Combine(buildDirectoryPath, "scriptconfig.json"),
            $@"{{
    ""output"": {{
        ""maxDepth"": {_settings.Results.MaxSerializationDepth},
        ""maxCollectionSerializeLength"": {_settings.Results.MaxCollectionSerializeLength}
    }}
}}");

        foreach (var referenceAssemblyImage in deployDependencies.AssemblyImageDependencies)
        {
            var fileName = referenceAssemblyImage.ConstructAssemblyFileName();

            await File.WriteAllBytesAsync(
                Path.Combine(buildDirectoryPath, fileName),
                referenceAssemblyImage.Image);
        }

        foreach (var referenceAssemblyPath in deployDependencies.AssemblyPathDependencies)
        {
            var destPath = Path.Combine(buildDirectoryPath, Path.GetFileName(referenceAssemblyPath));

            // Checking file exists means that the first assembly in the list of paths will win.
            // Later assemblies with the same file name will not be copied to the output directory.
            if (!File.Exists(destPath))
                File.Copy(referenceAssemblyPath, destPath, true);
        }

        foreach (var asset in deployDependencies.FileAssetsToCopy)
        {
            if (!asset.CopyFrom.Exists())
            {
                continue;
            }

            var copyTo = Path.GetFullPath(Path.Combine(buildDirectoryPath, asset.CopyTo.Path));

            if (!copyTo.StartsWith(buildDirectoryPath))
            {
                throw new Exception("Cannot copy asset to path outside the script start directory");
            }

            File.Copy(asset.CopyFrom.Path, copyTo, true);
        }

        return new DeploymentInfo(
            _script.GetFingerprint().CalculateHash(),
            scriptAssemblyFilePath.FileName,
            deployDependencies.ParsingResult.UserProgramStartLineNumber);
    }

    private string GenerateRuntimeConfigFileContents(DeployDependencies deployDependencies)
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

        var probingPaths =
            JsonSerializer.Serialize(deployDependencies.AssemblyPathDependencies.Select(Path.GetDirectoryName)
                .Distinct());

        return $$"""
                 {
                     "runtimeOptions": {
                         "tfm": "{{tfm}}",
                         "rollForward": "Minor",
                         "framework": {
                             "name": "{{frameworkName}}",
                             "version": "{{runtimeVersion}}"
                         },
                         "additionalProbingPaths": {{probingPaths}}
                     }
                 }
                 """;
    }

    private record DeployDependencies(
        CodeParsingResult ParsingResult,
        byte[] ScriptAssemblyBytes,
        List<AssemblyImage> AssemblyImageDependencies,
        HashSet<string> AssemblyPathDependencies,
        HashSet<FileAssetCopy> FileAssetsToCopy);
}
