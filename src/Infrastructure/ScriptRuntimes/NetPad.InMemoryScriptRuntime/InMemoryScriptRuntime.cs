using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Assemblies;
using NetPad.Compilation;
using NetPad.DotNet;
using NetPad.Exceptions;
using NetPad.IO;
using NetPad.Packages;
using NetPad.Scripts;
using NetPad.Utilities;

namespace NetPad.Runtimes;

/// <summary>
/// A script runtime that runs scripts in memory.
///
/// NOTE: If this class is unsealed, IDisposable and IAsyncDisposable implementations must be revised.
/// </summary>
public sealed class InMemoryScriptRuntime : IScriptRuntime<IScriptOutputAdapter<ScriptOutput, ScriptOutput>>
{
    internal record MainScriptOutputAdapter(IOutputWriter<ScriptOutput> ResultsChannel, IOutputWriter<ScriptOutput> SqlChannel)
        : ScriptOutputAdapter<ScriptOutput, ScriptOutput>(ResultsChannel, SqlChannel);

    private readonly Script _script;
    private readonly ICodeParser _codeParser;
    private readonly ICodeCompiler _codeCompiler;
    private readonly IPackageProvider _packageProvider;
    private readonly ILogger<InMemoryScriptRuntime> _logger;
    private readonly MainScriptOutputAdapter _outputAdapter;
    private readonly HashSet<IScriptOutputAdapter<ScriptOutput, ScriptOutput>> _externalOutputAdapters;
    private IServiceScope? _serviceScope;

    public InMemoryScriptRuntime(
        Script script,
        IServiceScope serviceScope,
        ICodeParser codeParser,
        ICodeCompiler codeCompiler,
        IPackageProvider packageProvider,
        ILogger<InMemoryScriptRuntime> logger)
    {
        _script = script;
        _serviceScope = serviceScope;
        _codeParser = codeParser;
        _codeCompiler = codeCompiler;
        _packageProvider = packageProvider;
        _logger = logger;
        _externalOutputAdapters = new HashSet<IScriptOutputAdapter<ScriptOutput, ScriptOutput>>();

        void ForwardToExternalAdapters<TScriptOutput>(
            Func<IScriptOutputAdapter<ScriptOutput, ScriptOutput>, IOutputWriter<TScriptOutput>?> channelGetter,
            TScriptOutput? output,
            string? title)
        {
            if (output == null)
            {
                return;
            }

            foreach (var adapter in _externalOutputAdapters)
            {
                try
                {
                    channelGetter(adapter)?.WriteAsync(output, title);
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
        try
        {
            var (success, assemblyBytes, referenceAssemblyImages, referenceAssemblyPaths, parsingResult) =
                await CompileAndGetReferencesAsync(runOptions);

            if (!success)
                return RunResult.RunAttemptFailure();

            var (alcWeakRef, completionSuccess, elapsedMs) = await ExecuteInMemoryAndUnloadAsync(
                _serviceScope!,
                assemblyBytes,
                referenceAssemblyImages,
                referenceAssemblyPaths,
                parsingResult.ParsedCodeInformation
            );

            for (int i = 0; alcWeakRef.IsAlive && i < 10; i++)
            {
                GCUtil.CollectAndWait();
            }

            _logger.LogDebug("alcWeakRef.IsAlive after GC collect?: " + alcWeakRef.IsAlive);

            return !completionSuccess ? RunResult.ScriptCompletionFailure(elapsedMs) : RunResult.Success(elapsedMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running script");
            await _outputAdapter.ResultsChannel.WriteAsync(new RawScriptOutput(ex + "\n"));
            return RunResult.RunAttemptFailure();
        }
    }

    public Task StopScriptAsync()
    {
        throw new InvalidOperationException("Cannot stop a script running in-memory.");
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
            HashSet<string> referenceAssemblyPaths,
            CodeParsingResult parsingResult)>
        CompileAndGetReferencesAsync(RunOptions runOptions)
    {
        var parsingResult = _codeParser.Parse(_script, new CodeParsingOptions
        {
            IncludedCode = runOptions.SpecificCodeToRun,
            AdditionalCode = runOptions.AdditionalCode
        });

        var referenceAssemblyImages = new HashSet<AssemblyImage>();
        foreach (var additionalReference in runOptions.AdditionalReferences)
        {
            if (additionalReference is AssemblyImageReference assemblyImageReference)
                referenceAssemblyImages.Add(assemblyImageReference.AssemblyImage);
        }

        var referenceAssemblyPaths = await _script.Config.References
            .Union(runOptions.AdditionalReferences)
            .GetAssemblyPathsAsync(_packageProvider);

        var fullProgram = parsingResult.GetFullProgram()
            .Replace("Console.WriteLine",
                $"{parsingResult.ParsedCodeInformation.BootstrapperClassName}.OutputWriteLine")
            .Replace("Console.Write", $"{parsingResult.ParsedCodeInformation.BootstrapperClassName}.OutputWrite");

        var compilationResult = _codeCompiler.Compile(new CompilationInput(
                fullProgram,
                referenceAssemblyImages.Select(a => a.Image).ToHashSet(),
                referenceAssemblyPaths)
            .WithOutputAssemblyNameTag(_script.Name));

        if (!compilationResult.Success)
        {
            await _outputAdapter.ResultsChannel.WriteAsync(new RawScriptOutput(
                compilationResult.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .JoinToString("\n") + "\n"));

            return (false, Array.Empty<byte>(), new HashSet<AssemblyImage>(), new HashSet<string>(), parsingResult);
        }

        return (
            true,
            compilationResult.AssemblyBytes,
            referenceAssemblyImages,
            referenceAssemblyPaths,
            parsingResult
        );
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private async Task<(WeakReference alcWeakRef, bool completionSuccess, double elapsedMs)>
        ExecuteInMemoryAndUnloadAsync(
            IServiceScope serviceScope,
            byte[] targetAssembly,
            HashSet<AssemblyImage> assemblyReferenceImages,
            HashSet<string> referenceAssemblyPaths,
            ParsedCodeInformation parsedCodeInformation)
    {
        using var scope = serviceScope.ServiceProvider.CreateScope();
        using var assemblyLoader = new UnloadableAssemblyLoader(
            assemblyReferenceImages,
            referenceAssemblyPaths,
            scope.ServiceProvider.GetRequiredService<ILogger<UnloadableAssemblyLoader>>()
        );

        var assembly = assemblyLoader.LoadFrom(targetAssembly);

        var alcWeakRef = new WeakReference(assemblyLoader, true);

        string bootstrapperClassName = parsedCodeInformation.BootstrapperClassName;
        Type? bootstrapperType = assembly.GetTypes().FirstOrDefault(t => t.Name == bootstrapperClassName);
        if (bootstrapperType == null)
        {
            throw new ScriptRuntimeException($"Could not find the bootstrapper type: {bootstrapperClassName}");
        }

        string setIOMethodName = parsedCodeInformation.BootstrapperSetIOMethodName;
        MethodInfo? setIOMethod =
            bootstrapperType.GetMethod(setIOMethodName, BindingFlags.Static | BindingFlags.NonPublic);
        if (setIOMethod == null)
        {
            throw new Exception(
                $"Could not find the entry method {setIOMethodName} on bootstrapper type: {bootstrapperClassName}");
        }

        setIOMethod.Invoke(null, new object?[] { _outputAdapter });

        MethodInfo? entryPoint = assembly.EntryPoint;
        if (entryPoint == null)
        {
            throw new ScriptRuntimeException("Could not find assembly entry point method.");
        }

        var runStart = DateTime.Now;

        try
        {
            _ = entryPoint.Invoke(null, new object?[] { Array.Empty<string>() });
        }
        catch (Exception ex)
        {
            await _outputAdapter.ResultsChannel.WriteAsync(new RawScriptOutput((ex.InnerException ?? ex).ToString()));
            return (alcWeakRef, false, GetElapsedMilliseconds(runStart));
        }

        return (alcWeakRef, true, GetElapsedMilliseconds(runStart));
    }

    public void Dispose()
    {
        _logger.LogTrace("Dispose start");

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

        _externalOutputAdapters.Clear();
        if (_serviceScope != null)
        {
            _serviceScope.Dispose();
            _serviceScope = null;
        }

        _logger.LogTrace("DisposeAsync end");
        return ValueTask.CompletedTask;
    }

    private double GetElapsedMilliseconds(DateTime start)
    {
        return (DateTime.Now - start).TotalMilliseconds;
    }
}
