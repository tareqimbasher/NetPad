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
using NetPad.Presentation;
using NetPad.Scripts;

namespace NetPad.ExecutionModel.InMemory;

/// <summary>
/// A script runtime that runs scripts in memory.
/// </summary>
[Obsolete("Unmaintained. Keeping it for now as it might be reborn as a new execution model that uses Client/Server")]
public sealed class InMemoryScriptRunner : IScriptRunner
{
    private readonly Script _script;
    private readonly ICodeParser _codeParser;
    private readonly ICodeCompiler _codeCompiler;
    private readonly IPackageProvider _packageProvider;
    private readonly ILogger<InMemoryScriptRunner> _logger;
    private readonly HashSet<IInputReader<string>> _externalInputAdapters;
    private readonly IOutputWriter<object> _output;
    private readonly HashSet<IOutputWriter<object>> _externalOutputAdapters;
    private IServiceScope? _serviceScope;

    public InMemoryScriptRunner(
        Script script,
        IServiceScope serviceScope,
        ICodeParser codeParser,
        ICodeCompiler codeCompiler,
        IPackageProvider packageProvider,
        ILogger<InMemoryScriptRunner> logger)
    {
        _script = script;
        _serviceScope = serviceScope;
        _codeParser = codeParser;
        _codeCompiler = codeCompiler;
        _packageProvider = packageProvider;
        _logger = logger;
        _externalInputAdapters = [];
        _externalOutputAdapters = [];

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
                referenceAssemblyPaths
            );

            for (int i = 0; alcWeakRef.IsAlive && i < 10; i++)
            {
                GcUtil.CollectAndWait();
            }

            _logger.LogDebug("alcWeakRef.IsAlive after GC collect?: " + alcWeakRef.IsAlive);

            return !completionSuccess ? RunResult.ScriptCompletionFailure(elapsedMs) : RunResult.Success(elapsedMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running script");
            await _output.WriteAsync(new RawScriptOutput(ex + "\n"));
            return RunResult.RunAttemptFailure();
        }
    }

    public Task StopScriptAsync()
    {
        throw new InvalidOperationException("Cannot stop a script running in-memory.");
    }

    public string[] GetUserVisibleAssemblies()
    {
        return [];
    }

    public void AddInput(IInputReader<string> inputReader)
    {
        _externalInputAdapters.Add(inputReader);
    }

    public void RemoveInput(IInputReader<string> inputReader)
    {
        _externalInputAdapters.Remove(inputReader);
    }

    public void AddOutput(IOutputWriter<object> outputWriter)
    {
        _externalOutputAdapters.Add(outputWriter);
    }

    public void RemoveOutput(IOutputWriter<object> outputWriter)
    {
        _externalOutputAdapters.Remove(outputWriter);
    }

    private async Task<(
            bool success,
            byte[] assemblyBytes,
            HashSet<AssemblyImage> referenceAssemblyImages,
            HashSet<string> referenceAssemblyPaths,
            CodeParsingResult parsingResult)>
        CompileAndGetReferencesAsync(RunOptions runOptions)
    {
        var parsingResult = _codeParser.Parse(
            runOptions.SpecificCodeToRun ?? _script.Code,
            _script.Config.Kind,
            _script.Config.Namespaces,
            new CodeParsingOptions
            {
                //AdditionalCode = runOptions.AdditionalCode
            });

        var referenceAssemblyImages = new HashSet<AssemblyImage>();
        // foreach (var additionalReference in runOptions.AdditionalReferences)
        // {
        //     if (additionalReference is AssemblyImageReference assemblyImageReference)
        //         referenceAssemblyImages.Add(assemblyImageReference.AssemblyImage);
        // }

        var assets = await _script.Config.References
            //.Union(runOptions.AdditionalReferences)
            .GetAssetsAsync(_script.Config.TargetFrameworkVersion, _packageProvider);

        var referenceAssemblyPaths = assets.Where(a => a.IsManagedAssembly).Select(a => a.Path).ToHashSet();

        var fullProgram = parsingResult.GetFullProgram()
            .ToCodeString()
            .Replace("Console.WriteLine",
                $"{InMemoryRunnerCSharpCodeParser.BootstrapperClassName}.OutputWriteLine")
            .Replace("Console.Write", $"{InMemoryRunnerCSharpCodeParser.BootstrapperClassName}.OutputWrite");

        var compilationResult = _codeCompiler.Compile(new CompilationInput(
                fullProgram,
                _script.Config.TargetFrameworkVersion,
                referenceAssemblyImages.Select(a => a.Image).ToHashSet(),
                referenceAssemblyPaths));

        if (!compilationResult.Success)
        {
            await _output.WriteAsync(new ErrorScriptOutput(
                compilationResult.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .JoinToString("\n") + "\n"));

            return (false, [], [], [], parsingResult);
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
            HashSet<string> referenceAssemblyPaths)
    {
        using var scope = serviceScope.ServiceProvider.CreateScope();
        using var assemblyLoader = new UnloadableAssemblyLoader(
            assemblyReferenceImages,
            referenceAssemblyPaths,
            scope.ServiceProvider.GetRequiredService<ILogger<UnloadableAssemblyLoader>>()
        );

        var assembly = assemblyLoader.LoadFrom(targetAssembly);

        var alcWeakRef = new WeakReference(assemblyLoader, true);

        string bootstrapperClassName = InMemoryRunnerCSharpCodeParser.BootstrapperClassName;
        Type? bootstrapperType = assembly.GetTypes().FirstOrDefault(t => t.Name == bootstrapperClassName);
        if (bootstrapperType == null)
        {
            throw new ScriptRuntimeException($"Could not find the bootstrapper type: {bootstrapperClassName}");
        }

        string setIOMethodName = InMemoryRunnerCSharpCodeParser.BootstrapperSetIOMethodName;
        MethodInfo? setIOMethod =
            bootstrapperType.GetMethod(setIOMethodName, BindingFlags.Static | BindingFlags.NonPublic);
        if (setIOMethod == null)
        {
            throw new Exception(
                $"Could not find the entry method {setIOMethodName} on bootstrapper type: {bootstrapperClassName}");
        }

        setIOMethod.Invoke(null, [_output]);

        MethodInfo? entryPoint = assembly.EntryPoint;
        if (entryPoint == null)
        {
            throw new ScriptRuntimeException("Could not find assembly entry point method.");
        }

        var runStart = DateTime.Now;

        try
        {
            _ = entryPoint.Invoke(null, [Array.Empty<string>()]);
        }
        catch (Exception ex)
        {
            await _output.WriteAsync(new ErrorScriptOutput((ex.InnerException ?? ex).ToString()));
            return (alcWeakRef, false, GetElapsedMilliseconds(runStart));
        }

        return (alcWeakRef, true, GetElapsedMilliseconds(runStart));
    }

    public void DumpMemCacheItem(string key)
    {
    }

    public void DeleteMemCacheItem(string key)
    {
    }

    public void ClearMemCacheItems()
    {
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

    private double GetElapsedMilliseconds(DateTime start)
    {
        return (DateTime.Now - start).TotalMilliseconds;
    }
}
