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

// If this class is unsealed, IDisposable and IAsyncDisposable implementations must be revised
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
            var compileResult = await CompileAndGetReferencesAsync(runOptions);

            if (!compileResult.success)
                return RunResult.RunAttemptFailure();

            var rootDir = _externalProcessSpawnRoot;

            var scriptName = StringUtils.RemoveInvalidFileNameCharacters(_script.Name).Replace(" ", "_");
            var assemblyFullPath = Path.Combine(rootDir.FullName, $"{scriptName}.dll");

            if (rootDir.Exists)
            {
                rootDir.Delete(true);
            }

            rootDir.Create();

            await File.WriteAllBytesAsync(assemblyFullPath, compileResult.assemblyBytes);

            var template = @"{{
    ""runtimeOptions"": {{
        ""framework"": {{
            ""name"": ""Microsoft.NETCore.App"",
            ""version"": ""6.0.0""
        }},
        ""rollForward"": ""Minor"",
        ""additionalProbingPaths"": {0}
    }}
}}";
            var runtimeConfig = string.Format(
                template,
                JsonSerializer.Serialize(compileResult.referenceAssemblyPaths.Select(Path.GetDirectoryName).ToHashSet())
            );

            await File.WriteAllTextAsync(Path.Combine(rootDir.FullName, $"{scriptName}.runtimeconfig.json"),
                runtimeConfig);

            foreach (var referenceAssemblyImage in compileResult.referenceAssemblyImages)
            {
                var fileName = referenceAssemblyImage.ConstructAssemblyFileName();

                await File.WriteAllBytesAsync(Path.Combine(rootDir.FullName, fileName), referenceAssemblyImage.Image);
            }

            foreach (var referenceAssemblyPath in compileResult.referenceAssemblyPaths)
            {
                // HACK: Needed to fix MS Build issue not copying the correct SqlClient assembly to output dir
                bool overwrite = Path.GetFileName(referenceAssemblyPath) != "Microsoft.Data.SqlClient.dll";

                var destPath = Path.Combine(rootDir.FullName, Path.GetFileName(referenceAssemblyPath));

                if (overwrite || !File.Exists(destPath))
                    File.Copy(referenceAssemblyPath, destPath, true);
            }

            // TODO optimize this section
            _processHandler = new ProcessHandler(DotNetInfo.LocateDotNetExecutableOrThrow(), assemblyFullPath);

            _processHandler.IO!.OnOutputReceivedHandlers.Add(async (output) =>
            {
                _logger.LogDebug("Script output received. Length: {OutputLength}",
                    (output?.Length.ToString() ?? "null"));

                ExternalProcessOutput<HtmlScriptOutput>? externalProcessOutput = null;

                if (output != null)
                {
                    try
                    {
                        externalProcessOutput =
                            JsonSerializer.Deserialize<ExternalProcessOutput<HtmlScriptOutput>>(output);
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
                else if (externalProcessOutput.Channel == ExternalProcessOutputChannel.Sql &&
                         _outputAdapter.SqlChannel != null)
                {
                    await _outputAdapter.SqlChannel.WriteAsync(externalProcessOutput.Output);
                }
            });

            _processHandler.IO!.OnErrorReceivedHandlers.Add(async (output) =>
            {
                await _outputAdapter.ResultsChannel.WriteAsync(new RawScriptOutput(output));
            });

            var start = DateTime.Now;

            var startResult = _processHandler.StartProcess();

            if (!startResult.Success)
            {
                return RunResult.RunAttemptFailure();
            }

            int exitCode = await startResult.WaitForExitTask;

            DisposeProcessHandler();

            var elapsed = (DateTime.Now - start).TotalMilliseconds;

            return exitCode == 0
                ? RunResult.Success((DateTime.Now - start).TotalMilliseconds)
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

    private async Task<(
            bool success,
            byte[] assemblyBytes,
            HashSet<AssemblyImage> referenceAssemblyImages,
            HashSet<string> referenceAssemblyPaths)>
        CompileAndGetReferencesAsync(RunOptions runOptions)
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
         * Compile assembly references
         */
        var referenceAssemblyImages = new HashSet<AssemblyImage>();
        foreach (var additionalReference in runOptions.AdditionalReferences)
        {
            if (additionalReference is AssemblyImageReference assemblyImageReference)
                referenceAssemblyImages.Add(assemblyImageReference.AssemblyImage);
        }

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
                referenceAssemblyImages.Select(a => a.Image),
                referenceAssemblyPaths)
            .WithOutputAssemblyNameTag(_script.Name));

        if (!compilationResult.Success)
        {
            await _outputAdapter.ResultsChannel.WriteAsync(new RawScriptOutput(compilationResult.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .JoinToString("\n") + "\n"));

            return (false, Array.Empty<byte>(), new HashSet<AssemblyImage>(), new HashSet<string>());
        }

        return (
            true,
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
}
