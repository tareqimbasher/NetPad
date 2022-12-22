using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Common;
using NetPad.Compilation;
using NetPad.Configuration;
using NetPad.DotNet;
using NetPad.IO;
using NetPad.Packages;
using NetPad.Scripts;
using NetPad.Utilities;

namespace NetPad.Runtimes;

/// <summary>
/// A script runtime that runs scripts in an external isolated process.
///
/// NOTE: If this class is unsealed, IDisposable and IAsyncDisposable implementations must be revised.
/// </summary>
public sealed class ExternalProcessScriptRuntime : IScriptRuntime<IScriptOutputAdapter<ScriptOutput, ScriptOutput>>
{
    internal record MainScriptOutputAdapter(IOutputWriter<ScriptOutput> ResultsChannel,
            IOutputWriter<ScriptOutput> SqlChannel)
        : ScriptOutputAdapter<ScriptOutput, ScriptOutput>(ResultsChannel, SqlChannel);

    private readonly Script _script;
    private readonly ICodeParser _codeParser;
    private readonly ICodeCompiler _codeCompiler;
    private readonly IPackageProvider _packageProvider;
    private readonly ILogger<ExternalProcessScriptRuntime> _logger;
    private readonly MainScriptOutputAdapter _outputAdapter;
    private readonly HashSet<IScriptOutputAdapter<ScriptOutput, ScriptOutput>> _externalOutputAdapters;
    private readonly DirectoryInfo _externalProcessSpawnRoot;
    private IServiceScope? _serviceScope;

    private ProcessHandler? _processHandler;

    public ExternalProcessScriptRuntime(
        Script script,
        IServiceScope serviceScope,
        ICodeParser codeParser,
        ICodeCompiler codeCompiler,
        IPackageProvider packageProvider,
        ILogger<ExternalProcessScriptRuntime> logger)
    {
        _script = script;
        _serviceScope = serviceScope;
        _codeParser = codeParser;
        _codeCompiler = codeCompiler;
        _packageProvider = packageProvider;
        _logger = logger;
        _externalOutputAdapters = new HashSet<IScriptOutputAdapter<ScriptOutput, ScriptOutput>>();
        _externalProcessSpawnRoot = Settings.TempFolderPath.Combine("processes", _script.Id.ToString()).GetInfo();

        // Used to forward output sent to the main _outputAdapter to any configured external output adapters
        void ForwardToExternalAdapters<TScriptOutput>(
            Func<IScriptOutputAdapter<ScriptOutput, ScriptOutput>, IOutputWriter<TScriptOutput>?> channelGetter,
            TScriptOutput? output,
            string? title)
        {
            if (output == null)
            {
                return;
            }

            foreach (var externalAdapter in _externalOutputAdapters)
            {
                try
                {
                    channelGetter(externalAdapter)?.WriteAsync(output, title);
                }
                catch
                {
                    // ignored
                }
            }
        }

        _outputAdapter = new MainScriptOutputAdapter(
            new ActionOutputWriter<ScriptOutput>((obj, title) => ForwardToExternalAdapters(
                o => o.ResultsChannel,
                obj,
                title)),
            new ActionOutputWriter<ScriptOutput>((obj, title) => ForwardToExternalAdapters(
                o => o.SqlChannel,
                obj,
                title))
        );
    }

    public async Task<RunResult> RunScriptAsync(RunOptions runOptions)
    {
        _logger.LogDebug("Starting to run script");

        try
        {
            var runDependencies = await GetRunDependencies(runOptions);

            if (runDependencies == null)
                return RunResult.RunAttemptFailure();

            var processRootFolder = _externalProcessSpawnRoot;

            if (processRootFolder.Exists)
            {
                processRootFolder.Delete(true);
            }

            processRootFolder.Create();

            var fileSafeScriptName = StringUtils.RemoveInvalidFileNameCharacters(_script.Name, "_").Replace(" ", "_");
            FilePath scriptAssemblyFilePath = Path.Combine(processRootFolder.FullName, $"{fileSafeScriptName}.dll");

            await File.WriteAllBytesAsync(scriptAssemblyFilePath.Path, runDependencies.ScriptAssemblyBytes);

            const string runtimeConfigFileTemplate = @"{{
    ""runtimeOptions"": {{
        ""framework"": {{
            ""name"": ""Microsoft.NETCore.App"",
            ""version"": ""6.0.0""
        }},
        ""rollForward"": ""Minor"",
        ""additionalProbingPaths"": {0}
    }}
}}";
            var runtimeConfigFileContents = string.Format(
                runtimeConfigFileTemplate,
                JsonSerializer.Serialize(runDependencies.AssemblyPathDependencies.Select(Path.GetDirectoryName).ToHashSet())
            );

            await File.WriteAllTextAsync(
                Path.Combine(processRootFolder.FullName, $"{fileSafeScriptName}.runtimeconfig.json"),
                runtimeConfigFileContents
            );

            foreach (var referenceAssemblyImage in runDependencies.AssemblyImageDependencies)
            {
                var fileName = referenceAssemblyImage.ConstructAssemblyFileName();

                await File.WriteAllBytesAsync(Path.Combine(processRootFolder.FullName, fileName), referenceAssemblyImage.Image);
            }

            foreach (var referenceAssemblyPath in runDependencies.AssemblyPathDependencies)
            {
                // HACK: Needed to fix MS Build issue not copying the correct SqlClient assembly to output dir
                bool overwrite = Path.GetFileName(referenceAssemblyPath) != "Microsoft.Data.SqlClient.dll";

                var destPath = Path.Combine(processRootFolder.FullName, Path.GetFileName(referenceAssemblyPath));

                // Aside from checking the overwrite flag here, checking file exists means that the first assembly
                // in the list of paths will win. Later assemblies with the same file name will not be copied to the
                // output directory.
                if (overwrite || !File.Exists(destPath))
                    File.Copy(referenceAssemblyPath, destPath, true);
            }

            // TODO optimize this section
            _processHandler = new ProcessHandler(DotNetInfo.LocateDotNetExecutableOrThrow(), scriptAssemblyFilePath.Path);

            _processHandler.IO!.OnOutputReceivedHandlers.Add(async (output) =>
            {
                _logger.LogDebug("Script output received. Length: {OutputLength}", (output?.Length.ToString() ?? "null"));

                ExternalProcessOutput<HtmlScriptOutput>? externalProcessOutput = null;

                if (output != null)
                {
                    try
                    {
                        externalProcessOutput = JsonSerializer.Deserialize<ExternalProcessOutput<HtmlScriptOutput>>(output);
                    }
                    catch
                    {
                        _logger.LogDebug("Script output is not JSON or could not be serialized");
                    }
                }

                if (externalProcessOutput?.Output == null)
                {
                    await _outputAdapter.ResultsChannel.WriteAsync(new RawScriptOutput(output));
                }
                else if (externalProcessOutput.Channel == ExternalProcessOutputChannel.Results)
                {
                    await _outputAdapter.ResultsChannel.WriteAsync(externalProcessOutput.Output);
                }
                else if (externalProcessOutput.Channel == ExternalProcessOutputChannel.Sql && _outputAdapter.SqlChannel != null)
                {
                    await _outputAdapter.SqlChannel.WriteAsync(externalProcessOutput.Output);
                }
            });

            _processHandler.IO!.OnErrorReceivedHandlers.Add(async (output) => { await _outputAdapter.ResultsChannel.WriteAsync(new RawScriptOutput(output)); });

            var start = DateTime.Now;

            var startResult = _processHandler.StartProcess();

            if (!startResult.Success)
            {
                return RunResult.RunAttemptFailure();
            }

            int exitCode = await startResult.WaitForExitTask;

            DisposeProcessHandler();

            var elapsed = (DateTime.Now - start).TotalMilliseconds;

            _logger.LogDebug("Script run completed with exit code: {ExitCode}. Duration: {Duration} ms", exitCode, elapsed);

            return exitCode == 0
                ? RunResult.Success(elapsed)
                : RunResult.ScriptCompletionFailure(elapsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running script");
            await _outputAdapter.ResultsChannel.WriteAsync(new RawScriptOutput(ex));
            return RunResult.RunAttemptFailure();
        }
    }

    public Task StopScriptAsync()
    {
        DisposeProcessHandler();
        return Task.CompletedTask;
    }

    private void DisposeProcessHandler()
    {
        if (_processHandler == null) return;
        _processHandler.Dispose();
        _processHandler = null;
    }

    public void AddOutput(IScriptOutputAdapter<ScriptOutput, ScriptOutput> outputAdapter)
    {
        _externalOutputAdapters.Add(outputAdapter);
    }

    public void RemoveOutput(IScriptOutputAdapter<ScriptOutput, ScriptOutput> outputAdapter)
    {
        _externalOutputAdapters.Remove(outputAdapter);
    }

    private async Task<RunDependencies?> GetRunDependencies(RunOptions runOptions)
    {
        /*
         * Parse script code into the code that will be compiled
         */
        var parsingResult = _codeParser.Parse(_script, new CodeParsingOptions
        {
            IncludedCode = runOptions.SpecificCodeToRun,
            AdditionalCode = runOptions.AdditionalCode
        });

        var fullProgram = parsingResult.GetFullProgram();

        /*
         * Gather assembly references
         */
        // Images
        var referenceAssemblyImages = new HashSet<AssemblyImage>();
        foreach (var additionalReference in runOptions.AdditionalReferences)
        {
            if (additionalReference is AssemblyImageReference assemblyImageReference)
                referenceAssemblyImages.Add(assemblyImageReference.AssemblyImage);
        }

        // File paths
        var referenceAssemblyPaths = (await _script.Config.References
                .Union(runOptions.AdditionalReferences)
                .GetAssemblyPathsAsync(_packageProvider))
            .ToHashSet();

        /*
         * Add custom assemblies
         */
        referenceAssemblyPaths.Add(typeof(IOutputWriter<>).Assembly.Location);
        // Needed to serialize output in external process to HTML
        referenceAssemblyPaths.Add(typeof(O2Html.HtmlConvert).Assembly.Location);


        /*
         * Compile
         */
        var compilationResult = _codeCompiler.Compile(new CompilationInput(
                fullProgram,
                referenceAssemblyImages.Select(a => a.Image).ToHashSet(),
                referenceAssemblyPaths)
            .WithOutputAssemblyNameTag(_script.Name));

        if (!compilationResult.Success)
        {
            await _outputAdapter.ResultsChannel.WriteAsync(new RawScriptOutput(compilationResult.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .JoinToString("\n") + "\n"));

            return null;
        }

        return new RunDependencies(
            compilationResult.AssemblyBytes,
            referenceAssemblyImages,
            referenceAssemblyPaths
        );
    }

    public void Dispose()
    {
        _logger.LogTrace("Dispose start");

        _processHandler?.Dispose();
        _externalOutputAdapters.Clear();
        if (_serviceScope != null)
        {
            _serviceScope.Dispose();
            _serviceScope = null;
        }

        _logger.LogTrace("Dispose end");
    }

    public ValueTask DisposeAsync()
    {
        _logger.LogTrace("DisposeAsync start");

        _processHandler?.Dispose();
        _externalOutputAdapters.Clear();
        if (_serviceScope != null)
        {
            _serviceScope.Dispose();
            _serviceScope = null;
        }

        _logger.LogTrace("DisposeAsync end");
        return ValueTask.CompletedTask;
    }

    private record RunDependencies(
        byte[] ScriptAssemblyBytes,
        HashSet<AssemblyImage> AssemblyImageDependencies,
        HashSet<string> AssemblyPathDependencies);
}
