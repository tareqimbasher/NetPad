using System.Reflection;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using NetPad.Compilation;
using NetPad.Configuration;
using NetPad.DotNet;
using NetPad.IO;
using NetPad.Packages;
using NetPad.Scripts;
using NetPad.Utilities;
using O2Html;
using HtmlSerializer = NetPad.Html.HtmlSerializer;
using JsonSerializer = NetPad.Common.JsonSerializer;

namespace NetPad.Runtimes;

/// <summary>
/// A script runtime that runs scripts in an external isolated process.
///
/// NOTE: If this class is unsealed, IDisposable and IAsyncDisposable implementations must be revised.
/// </summary>
public sealed class ExternalProcessScriptRuntime : IScriptRuntime
{
    private const string RuntimeConfigFileTemplate = @"{{
    ""runtimeOptions"": {{
        ""tfm"": ""{0}"",
        ""framework"": {{
            ""name"": ""Microsoft.NETCore.App"",
            ""version"": ""{1}""
        }},
        ""rollForward"": ""Minor"",
        ""additionalProbingPaths"": {2}
    }}
}}";

    private readonly Script _script;
    private readonly ICodeParser _codeParser;
    private readonly ICodeCompiler _codeCompiler;
    private readonly IPackageProvider _packageProvider;
    private readonly IDotNetInfo _dotNetInfo;
    private readonly Settings _settings;
    private readonly ILogger<ExternalProcessScriptRuntime> _logger;
    private readonly HashSet<IInputReader<string>> _externalInputReaders;
    private readonly IOutputWriter<object> _output;
    private readonly HashSet<IOutputWriter<object>> _externalOutputAdapters;
    private readonly DirectoryInfo _externalProcessRootDirectory;
    private readonly RawOutputHandler _rawOutputHandler;

    private ProcessHandler? _processHandler;

    public ExternalProcessScriptRuntime(
        Script script,
        ICodeParser codeParser,
        ICodeCompiler codeCompiler,
        IPackageProvider packageProvider,
        IDotNetInfo dotNetInfo,
        Settings settings,
        ILogger<ExternalProcessScriptRuntime> logger)
    {
        _script = script;
        _codeParser = codeParser;
        _codeCompiler = codeCompiler;
        _packageProvider = packageProvider;
        _dotNetInfo = dotNetInfo;
        _settings = settings;
        _logger = logger;
        _externalInputReaders = new HashSet<IInputReader<string>>();
        _externalOutputAdapters = new HashSet<IOutputWriter<object>>();
        _externalProcessRootDirectory = AppDataProvider.ExternalProcessesDirectoryPath
            .Combine(_script.Id.ToString())
            .GetInfo();

        // Forward output to any configured external output adapters
        _output = new AsyncActionOutputWriter<object>(async (output, title) =>
        {
            if (output == null)
            {
                return;
            }

            foreach (var externalAdapter in _externalOutputAdapters)
            {
                try
                {
                    await externalAdapter.WriteAsync(output, title);
                }
                catch
                {
                    // ignored
                }
            }
        });

        _rawOutputHandler = new RawOutputHandler(_output);
    }

    public async Task<RunResult> RunScriptAsync(RunOptions runOptions)
    {
        _logger.LogDebug("Starting to run script");

        try
        {
            var runDependencies = await GetRunDependencies(runOptions);

            if (runDependencies == null)
                return RunResult.RunAttemptFailure();

            // Create a new dir for each run
            _externalProcessRootDirectory.Refresh();

            if (_externalProcessRootDirectory.Exists)
            {
                _externalProcessRootDirectory.Delete(true);
            }

            _externalProcessRootDirectory.Create();

            var scriptAssemblyFilePath = await SetupExternalProcessFolderAsync(_externalProcessRootDirectory, runDependencies);

            // Reset raw output order
            _rawOutputHandler.Reset();

            // TODO optimize this section
            _processHandler = new ProcessHandler(_dotNetInfo.LocateDotNetExecutableOrThrow(), scriptAssemblyFilePath.Path);

            _processHandler.IO.OnOutputReceivedHandlers.Add(async raw =>
            {
                if (raw == "[INPUT_REQUEST]")
                {
                    _logger.LogDebug("Input Request received");

                    // The first reader that returns a non-null input will be used
                    string? input = null;
                    foreach (var inputReader in _externalInputReaders)
                    {
                        input = await inputReader.ReadAsync();
                        if (input != null)
                        {
                            break;
                        }
                    }

                    await _processHandler.IO.StandardInput.WriteLineAsync(input);
                    return;
                }

                _logger.LogDebug("Script output received. Length: {OutputLength}", raw.Length.ToString());

                JsonElement packetJson;
                string type;
                JsonElement outputProperty;

                try
                {
                    packetJson = JsonDocument.Parse(raw).RootElement;
                    type = packetJson.GetProperty(nameof(ExternalProcessOutput.Type).ToLowerInvariant()).GetString() ?? string.Empty;
                    outputProperty = packetJson.GetProperty(nameof(ExternalProcessOutput.Output).ToLowerInvariant());
                }
                catch
                {
                    _logger.LogDebug("Script output is not JSON or could not be deserialized. Output: '{RawOutput}'", raw);
                    _rawOutputHandler.RawErrorOutputReceived(raw);
                    return;
                }

                ScriptOutput output;

                if (type == nameof(HtmlResultsScriptOutput))
                {
                    output = JsonSerializer.Deserialize<HtmlResultsScriptOutput>(outputProperty.ToString())
                             ?? throw new FormatException($"Could deserialize JSON to {nameof(HtmlResultsScriptOutput)}");
                }
                else if (type == nameof(HtmlSqlScriptOutput))
                {
                    output = JsonSerializer.Deserialize<HtmlSqlScriptOutput>(outputProperty.ToString())
                             ?? throw new FormatException($"Could deserialize JSON to {nameof(HtmlSqlScriptOutput)}");
                }
                else if (type == nameof(HtmlErrorScriptOutput))
                {
                    output = JsonSerializer.Deserialize<HtmlErrorScriptOutput>(outputProperty.ToString())
                             ?? throw new FormatException($"Could deserialize JSON to {nameof(HtmlErrorScriptOutput)}");
                }
                else
                {
                    _rawOutputHandler.RawResultOutputReceived(raw);
                    return;
                }

                await _output.WriteAsync(output);
            });

            _processHandler.IO.OnErrorReceivedHandlers.Add(output =>
            {
                _rawOutputHandler.RawErrorOutputReceived(output);
                return Task.CompletedTask;
            });

            var start = DateTime.Now;

            var startResult = _processHandler.StartProcess();

            if (!startResult.Success)
            {
                return RunResult.RunAttemptFailure();
            }

            var exitCode = await startResult.WaitForExitTask;

            var elapsed = (DateTime.Now - start).TotalMilliseconds;

            _logger.LogDebug(
                "Script run completed with exit code: {ExitCode}. Duration: {Duration} ms",
                exitCode,
                elapsed);

            return exitCode switch
            {
                0 => RunResult.Success(elapsed),
                -1 => RunResult.RunCancelled(),
                _ => RunResult.ScriptCompletionFailure(elapsed)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running script");
            await _output.WriteAsync(new ErrorScriptOutput(ex));
            return RunResult.RunAttemptFailure();
        }
        finally
        {
            StopAndCleanup();
        }
    }

    private async Task<FilePath> SetupExternalProcessFolderAsync(
        DirectoryInfo processRootFolder,
        RunDependencies runDependencies)
    {
        var fileSafeScriptName = StringUtil
            .RemoveInvalidFileNameCharacters(_script.Name, "_")
            .Replace(" ", "_");

        FilePath scriptAssemblyFilePath = Path.Combine(processRootFolder.FullName, $"{fileSafeScriptName}.dll");

        await File.WriteAllBytesAsync(scriptAssemblyFilePath.Path, runDependencies.ScriptAssemblyBytes);

        await File.WriteAllTextAsync(
            Path.Combine(processRootFolder.FullName, $"{fileSafeScriptName}.runtimeconfig.json"),
            GenerateRuntimeConfigFileContents(runDependencies)
        );

        await File.WriteAllTextAsync(
            Path.Combine(processRootFolder.FullName, "scriptconfig.json"),
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
                Path.Combine(processRootFolder.FullName, fileName),
                referenceAssemblyImage.Image);
        }

        foreach (var referenceAssemblyPath in runDependencies.AssemblyPathDependencies)
        {
            var destPath = Path.Combine(processRootFolder.FullName, Path.GetFileName(referenceAssemblyPath));

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

            var copyTo = Path.Combine(processRootFolder.FullName, asset.CopyTo.Path);
            File.Copy(asset.CopyFrom.Path, copyTo, true);
        }

        return scriptAssemblyFilePath;
    }

    private async Task<RunDependencies?> GetRunDependencies(RunOptions runOptions)
    {
        //
        // Add code that initializes runtime services
        //
        runOptions.AdditionalCode.Add(new SourceCode("public partial class Program " +
                                                     $"{{ static Program() {{ {nameof(ScriptRuntimeServices)}.{nameof(ScriptRuntimeServices.UseStandardIO)}(); }} }}"));

        //
        // Gather assembly references
        //
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
                Path = asset.Path,
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

        //
        // Add custom assemblies
        //
        referenceAssemblyPaths.Add(typeof(IOutputWriter<>).Assembly.Location);
        // Needed to serialize output in external process to HTML
        referenceAssemblyPaths.Add(typeof(HtmlConvert).Assembly.Location);
        referenceAssemblyPaths.Add(typeof(HtmlSerializer).Assembly.Location);


        //
        // Parse Code & Compile
        //
        var compilationResult = ParseAndCompile(
            runOptions.SpecificCodeToRun ?? _script.Code,
            referenceAssemblyImages.Select(a => a.Image).ToHashSet(),
            referenceAssemblyPaths,
            runOptions.AdditionalCode);

        if (!compilationResult.Success)
        {
            await _output.WriteAsync(new ErrorScriptOutput(compilationResult
                .Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .JoinToString("\n") + "\n"));

            return null;
        }

        var runAssets = new HashSet<RunAsset>(runOptions.Assets);

        foreach (var asset in referenceAssets.Where(x => !x.IsAssembly))
        {
            runAssets.Add(new RunAsset(asset.Path, $"./{Path.GetFileName(asset.Path)}"));
        }

        return new RunDependencies(
            compilationResult.AssemblyBytes,
            referenceAssemblyImages,
            referenceAssemblyPaths,
            runAssets
        );
    }

    private CompilationResult ParseAndCompile(
        string code,
        HashSet<byte[]> referenceAssemblyImages,
        HashSet<string> referenceAssemblyPaths,
        SourceCodeCollection additionalCode)
    {
        CompilationResult parseAndCompile(string targetCode)
        {
            var parsingResult = _codeParser.Parse(
                targetCode,
                _script.Config.Kind,
                _script.Config.Namespaces,
                new CodeParsingOptions
                {
                    AdditionalCode = additionalCode
                });

            var fullProgram = parsingResult.GetFullProgram();

            return _codeCompiler.Compile(new CompilationInput(
                    fullProgram,
                    _script.Config.TargetFrameworkVersion,
                    referenceAssemblyImages,
                    referenceAssemblyPaths)
                .WithOutputAssemblyNameTag(_script.Name));
        }

        // We want to try code as-is, but also try additional permutations of it if it fails to compile
        var permutations = new List<Func<(bool shouldAttempt, string code)>>
        {
            () => (true, code),
            () =>
            {
                var trimmedCode = code.Trim();
                if (!trimmedCode.EndsWith(";") && !trimmedCode.EndsWith(".Dump()")) return (true, $"({trimmedCode}).Dump();");
                return (false, code);
            },
            () =>
            {
                var trimmedCode = code.Trim();
                return !trimmedCode.EndsWith(";") ? (true, trimmedCode + ";") : (false, code);
            }
        };

        CompilationResult? firstCompilationResult = null;
        CompilationResult? compilationResult;

        for (var ixPerm = 0; ixPerm < permutations.Count; ixPerm++)
        {
            var permutationFunc = permutations[ixPerm];
            var permutation = permutationFunc();
            if (!permutation.shouldAttempt) continue;

            compilationResult = parseAndCompile(permutation.code);

            if (ixPerm == 0) firstCompilationResult = compilationResult;

            if (compilationResult.Success)
            {
                return compilationResult;
            }
        }

        // If we got here compilation failed
        compilationResult = firstCompilationResult ?? throw new Exception($"Expected {nameof(firstCompilationResult)} to have a value");

        return compilationResult;
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

        return string.Format(
            RuntimeConfigFileTemplate,
            _script.Config.TargetFrameworkVersion.GetTargetFrameworkMoniker(),
            runtimeVersion,
            JsonSerializer.Serialize(runDependencies.AssemblyPathDependencies.Select(Path.GetDirectoryName).ToHashSet())
        );
    }

    public Task StopScriptAsync()
    {
        StopAndCleanup();
        return Task.CompletedTask;
    }

    private void StopAndCleanup()
    {
        if (_processHandler == null) return;

        try
        {
            _processHandler.Dispose();
            _processHandler = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing process handler");
        }

        _externalProcessRootDirectory.Refresh();

        if (_externalProcessRootDirectory.Exists)
        {
            try
            {
                _externalProcessRootDirectory.Delete(true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not delete process root directory: {Path}. Error: {ErrorMessage}",
                    _externalProcessRootDirectory.FullName,
                    ex.Message);
            }
        }
    }

    public void AddInput(IInputReader<string> inputReader)
    {
        _externalInputReaders.Add(inputReader);
    }

    public void RemoveInput(IInputReader<string> inputReader)
    {
        _externalInputReaders.Remove(inputReader);
    }

    public void AddOutput(IOutputWriter<object> outputAdapter)
    {
        _externalOutputAdapters.Add(outputAdapter);
    }

    public void RemoveOutput(IOutputWriter<object> outputAdapter)
    {
        _externalOutputAdapters.Remove(outputAdapter);
    }

    public void Dispose()
    {
        _logger.LogTrace("Dispose start");

        _processHandler?.Dispose();
        _externalOutputAdapters.Clear();

        _logger.LogTrace("Dispose end");
    }

    private record RunDependencies(
        byte[] ScriptAssemblyBytes,
        HashSet<AssemblyImage> AssemblyImageDependencies,
        HashSet<string> AssemblyPathDependencies,
        HashSet<RunAsset> Assets);
}
