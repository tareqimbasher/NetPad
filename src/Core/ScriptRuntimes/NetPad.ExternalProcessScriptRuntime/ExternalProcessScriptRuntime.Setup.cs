using System.Reflection;
using Dumpify;
using Microsoft.CodeAnalysis;
using NetPad.Common;
using NetPad.Compilation;
using NetPad.Configuration;
using NetPad.DotNet;
using NetPad.IO;
using NetPad.Packages;
using NetPad.Utilities;
using Spectre.Console;

namespace NetPad.Runtimes;

public partial class ExternalProcessScriptRuntime
{
    private readonly ICodeParser _codeParser;
    private readonly ICodeCompiler _codeCompiler;
    private readonly IPackageProvider _packageProvider;
    private readonly Settings _settings;

    private async Task<RunDependencies?> GetRunDependencies(RunOptions runOptions)
    {
        // Images
        var referenceAssemblyImages = new HashSet<AssemblyImage>();
        foreach (var additionalReference in runOptions.AdditionalReferences)
        {
            if (additionalReference is AssemblyImageReference assemblyImageReference)
                referenceAssemblyImages.Add(assemblyImageReference.AssemblyImage);
        }

        // Files
        var referenceAssets = (await _script.Config.References
                .Union(runOptions.AdditionalReferences)
                .GetAssetsAsync(_script.Config.TargetFrameworkVersion, _packageProvider))
            .Select(asset => new
            {
                asset.Path,
                IsAssembly = asset.IsAssembly()
            })
            .ToArray();

        var referenceAssemblyPaths = referenceAssets
            .Where(x => x.IsAssembly)
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

        // Add app assemblies needed to support running external process
        foreach (var assemblyPath in GetUserAccessibleAssemblies())
        {
            referenceAssemblyPaths.Add(assemblyPath);
        }

        // Needed as dependencies to NetPad.Presentation assembly
        referenceAssemblyPaths.Add(typeof(DumpExtensions).Assembly.Location);
        referenceAssemblyPaths.Add(typeof(IAnsiConsole).Assembly.Location);

        // Parse Code & Compile
        var (parsingResult, compilationResult) = ParseAndCompile(
            runOptions.SpecificCodeToRun ?? _script.Code,
            referenceAssemblyImages,
            referenceAssemblyPaths,
            runOptions.AdditionalCode);

        if (!compilationResult.Success)
        {
            var error = compilationResult
                .Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => CorrectDiagnosticErrorLineNumber(d, parsingResult.UserProgramStartLineNumber));

            await _output.WriteAsync(new ErrorScriptOutput(error.JoinToString("\n") + "\n"));

            return null;
        }

        var runAssets = new HashSet<RunAsset>(runOptions.Assets);

        foreach (var asset in referenceAssets.Where(x => !x.IsAssembly))
        {
            runAssets.Add(new RunAsset(asset.Path, $"./{Path.GetFileName(asset.Path)}"));
        }

        return new RunDependencies(
            parsingResult,
            compilationResult.AssemblyBytes,
            referenceAssemblyImages,
            referenceAssemblyPaths,
            runAssets
        );
    }

    private ParseAndCompileResult ParseAndCompile(
        string code,
        HashSet<AssemblyImage> referenceAssemblyImages,
        HashSet<string> referenceAssemblyPaths,
        SourceCodeCollection additionalCode)
    {
        ParseAndCompileResult parseAndCompile(string targetCode)
        {
            var parsingResult = _codeParser.Parse(
                targetCode,
                _script.Config.Kind,
                _script.Config.Namespaces,
                new CodeParsingOptions
                {
                    AdditionalCode = additionalCode
                });

            parsingResult.BootstrapperProgram.Code.Update(parsingResult.BootstrapperProgram.Code.Value?
                .Replace("SCRIPT_ID", _script.Id.ToString())
                .Replace("SCRIPT_NAME", _script.Name)
                .Replace("SCRIPT_LOCATION", _script.Path));

            var fullProgram = parsingResult.GetFullProgram();

            var compilationResult = _codeCompiler.Compile(new CompilationInput(
                    fullProgram.ToCodeString(),
                    _script.Config.TargetFrameworkVersion,
                    referenceAssemblyImages.Select(a => a.Image).ToHashSet(),
                    referenceAssemblyPaths)
                .WithOutputAssemblyNameTag(_script.Name));

            return new ParseAndCompileResult(parsingResult, compilationResult);
        }

        // We will try different permutations of the user's code, starting with running it as-is. The idea is to account
        // for, and give the ability for users to, run expressions not ending with semi-colon or .Dump() and still produce
        // the "missing" pieces to run the expression.
        var permutations = new List<Func<(bool shouldAttempt, string code)>>
        {
            // As-is
            () => (true, code),

            // Try adding ".Dump();" to dump the result of an expression
            // There is no good way that I've found to determine if expression returns void or otherwise
            // so the only way to test if the expression results in a value is to try to compile it.
            () =>
            {
                var trimmedCode = code.Trim();

                if (!trimmedCode.EndsWith(";") && !trimmedCode.EndsWith(".Dump()"))
                {
                    return (true, $"({trimmedCode}).Dump();");
                }

                return (false, code);
            },

            // Try adding ";" to execute an expression
            () =>
            {
                var trimmedCode = code.Trim();
                return !trimmedCode.EndsWith(";")
                    ? (true, trimmedCode + ";")
                    : (false, code);
            }
        };

        ParseAndCompileResult? asIsResult = null;

        for (var ixPerm = 0; ixPerm < permutations.Count; ixPerm++)
        {
            var permutationFunc = permutations[ixPerm];
            var permutation = permutationFunc();

            if (!permutation.shouldAttempt)
            {
                continue;
            }

            var result = parseAndCompile(permutation.code);

            if (result.CompilationResult.Success)
            {
                return result;
            }

            if (ixPerm == 0)
            {
                asIsResult = result;
            }
        }

        // If we got here compilation failed
        return asIsResult!;
    }

    private async Task<FilePath> SetupExternalProcessRootDirectoryAsync(RunDependencies runDependencies)
    {
        // Create a new dir for each run
        _externalProcessRootDirectory.Refresh();

        if (_externalProcessRootDirectory.Exists)
        {
            _externalProcessRootDirectory.Delete(true);
        }

        _externalProcessRootDirectory.Create();

        // Write compiled assembly to dir
        var fileSafeScriptName = StringUtil
            .RemoveInvalidFileNameCharacters(_script.Name, "_")
            .Replace(" ", "_");

        FilePath scriptAssemblyFilePath = Path.Combine(_externalProcessRootDirectory.FullName, $"{fileSafeScriptName}.dll");

        await File.WriteAllBytesAsync(scriptAssemblyFilePath.Path, runDependencies.ScriptAssemblyBytes);

        // A runtimeconfig.json file tells .NET how to run the assembly
        await File.WriteAllTextAsync(
            Path.Combine(_externalProcessRootDirectory.FullName, $"{fileSafeScriptName}.runtimeconfig.json"),
            GenerateRuntimeConfigFileContents(runDependencies)
        );

        // The scriptconfig.json is custom and passes some options to the running script
        await File.WriteAllTextAsync(
            Path.Combine(_externalProcessRootDirectory.FullName, "scriptconfig.json"),
            $@"{{
    ""output"": {{
        ""maxDepth"": {_settings.Results.MaxSerializationDepth},
        ""maxCollectionSerializeLength"": {_settings.Results.MaxCollectionSerializeLength}
    }}
}}");

        foreach (var referenceAssemblyImage in runDependencies.AssemblyImageDependencies)
        {
            var fileName = referenceAssemblyImage.ConstructAssemblyFileName();

            await File.WriteAllBytesAsync(
                Path.Combine(_externalProcessRootDirectory.FullName, fileName),
                referenceAssemblyImage.Image);
        }

        foreach (var referenceAssemblyPath in runDependencies.AssemblyPathDependencies)
        {
            var destPath = Path.Combine(_externalProcessRootDirectory.FullName, Path.GetFileName(referenceAssemblyPath));

            // Checking file exists means that the first assembly in the list of paths will win.
            // Later assemblies with the same file name will not be copied to the output directory.
            if (!File.Exists(destPath))
                File.Copy(referenceAssemblyPath, destPath, true);
        }

        foreach (var asset in runDependencies.Assets)
        {
            if (!asset.CopyFrom.Exists())
            {
                continue;
            }

            var copyTo = Path.Combine(_externalProcessRootDirectory.FullName, asset.CopyTo.Path);
            File.Copy(asset.CopyFrom.Path, copyTo, true);
        }

        return scriptAssemblyFilePath;
    }

    private string GenerateRuntimeConfigFileContents(RunDependencies runDependencies)
    {
        var runtimeVersion = _dotNetInfo.GetDotNetRuntimeVersionsOrThrow()
            .Where(v =>
                v.FrameworkName == "Microsoft.NETCore.App"
                && v.Version.Major == _script.Config.TargetFrameworkVersion.GetMajorVersion())
            .MaxBy(v => v.Version)?
            .Version;

        if (runtimeVersion == null)
            throw new Exception($"Could not find a .NET {_script.Config.TargetFrameworkVersion.GetMajorVersion()} runtime");

        var tfm = _script.Config.TargetFrameworkVersion.GetTargetFrameworkMoniker();
        var probingPaths = JsonSerializer.Serialize(runDependencies.AssemblyPathDependencies.Select(Path.GetDirectoryName).Distinct());

        return $@"{{
    ""runtimeOptions"": {{
        ""tfm"": ""{tfm}"",
        ""framework"": {{
            ""name"": ""Microsoft.NETCore.App"",
            ""version"": ""{runtimeVersion}""
        }},
        ""rollForward"": ""Minor"",
        ""additionalProbingPaths"": {probingPaths}
    }}
}}";
    }

    /// <summary>
    /// Corrects line numbers in compilation errors.
    /// </summary>
    private static string CorrectDiagnosticErrorLineNumber(Diagnostic diagnostic, int userProgramStartLineNumber)
    {
        var err = diagnostic.ToString();

        if (!err.StartsWith('('))
        {
            return err;
        }

        var errParts = err.Split(':');
        var span = errParts.First().Trim(new[] { '(', ')' });
        var spanParts = span.Split(',');
        var lineNumberStr = spanParts[0];

        return int.TryParse(lineNumberStr, out int lineNumber)
            ? $"({lineNumber - userProgramStartLineNumber},{spanParts[1]}):{errParts.Skip(1).JoinToString(":")}"
            : err;
    }

    private record RunDependencies(
        CodeParsingResult ParsingResult,
        byte[] ScriptAssemblyBytes,
        HashSet<AssemblyImage> AssemblyImageDependencies,
        HashSet<string> AssemblyPathDependencies,
        HashSet<RunAsset> Assets);

    private record ParseAndCompileResult(CodeParsingResult ParsingResult, CompilationResult CompilationResult);
}
