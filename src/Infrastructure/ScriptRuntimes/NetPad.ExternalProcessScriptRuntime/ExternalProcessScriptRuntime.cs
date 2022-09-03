using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Compilation;
using NetPad.DotNet;
using NetPad.IO;
using NetPad.Packages;
using NetPad.Scripts;
using NetPad.Utilities;

namespace NetPad.Runtimes;

// If this class is unsealed, IDisposable and IAsyncDisposable implementations must be revised
public sealed class ExternalProcessScriptRuntime : IScriptRuntime
{
    private readonly Script _script;
    private readonly ICodeParser _codeParser;
    private readonly ICodeCompiler _codeCompiler;
    private readonly IPackageProvider _packageProvider;
    private readonly ILogger<ExternalProcessScriptRuntime> _logger;
    private readonly IOutputWriter _outputWriter;
    private readonly HashSet<IOutputWriter> _outputListeners;
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
        _outputListeners = new HashSet<IOutputWriter>();

        _outputWriter = new ActionOutputWriter((obj, title) =>
        {
            foreach (var outputWriter in _outputListeners)
            {
                try
                {
                    outputWriter.WriteAsync(obj, title);
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
        throw new NotImplementedException("Testing this runtime is not yet complete");
        var dir = "/home/tips/test/";

        var (success, assemblyBytes, referenceAssemblyPaths) = await CompileAndGetRefAssemblyPathsAsync();

        if (!success)
            return RunResult.RunAttemptFailure();

        var scriptName = _script.Name.Replace(" ", "_");
        var assemblyFullPath = Path.Combine(dir, $"{scriptName}.dll");

        await File.WriteAllBytesAsync(assemblyFullPath, assemblyBytes);

        var runtimeConfig = string.Format(@"{{
    ""runtimeOptions"": {{
        ""framework"": {{
            ""name"": ""Microsoft.NETCore.App"",
            ""version"": ""6.0.0""
        }},
        ""rollForward"": ""Minor"",
        ""additionalProbingPaths"": [
            {0}
        ]
    }}
}}", referenceAssemblyPaths.Select(x => $"\"{Path.GetDirectoryName(x)}\"").JoinToString(",\n"));
        await File.WriteAllTextAsync(Path.Combine(dir, $"{scriptName}.runtimeconfig.json"), runtimeConfig);

        var domainDll = typeof(IOutputWriter).Assembly.Location;
        File.Copy(domainDll, Path.Combine(dir, Path.GetFileName(domainDll)), true);

        foreach (var referenceAssemblyPath in referenceAssemblyPaths)
        {
            File.Copy(referenceAssemblyPath, Path.Combine(dir, Path.GetFileName(referenceAssemblyPath)), true);
        }

        _processHandler = new IO.ProcessHandler("dotnet", assemblyFullPath);
        _processHandler.Init();
        _processHandler.ProcessIO!.OnOutputReceivedHandlers.Add(async (t) =>
        {
            await _outputWriter.WriteAsync(t);
        });
        _processHandler.ProcessIO!.OnErrorReceivedHandlers.Add(async (t) =>
        {
            await _outputWriter.WriteAsync(t);
        });

        var start = DateTime.Now;

        var runSuccess = await _processHandler.RunAsync(true);
        return runSuccess ? RunResult.Success((DateTime.Now - start).TotalMilliseconds) : RunResult.RunAttemptFailure();
    }

    private async Task<(bool success, byte[] assemblyBytes, string[] referenceAssemblyPaths)> CompileAndGetRefAssemblyPathsAsync()
    {
        var parsingResult = _codeParser.Parse(_script);

        var referenceAssemblyPaths = await GetReferenceAssemblyPathsAsync();

        //
        var fullProgram = parsingResult.GetFullProgram()
            .Replace("Console.WriteLine", "Program.OutputWriteLine")
            .Replace("Console.Write", "Program.OutputWrite");

        var compilationResult = _codeCompiler.Compile(
            new CompilationInput(fullProgram, referenceAssemblyPaths).WithOutputAssemblyNameTag(_script.Name));

        if (!compilationResult.Success)
        {
            await _outputWriter.WriteAsync(compilationResult.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .JoinToString("\n") + "\n");

            return (false, Array.Empty<byte>(), Array.Empty<string>());
        }

        return (true, compilationResult.AssemblyBytes, referenceAssemblyPaths);
    }

    private async Task<string[]> GetReferenceAssemblyPathsAsync()
    {
        var assemblyPaths = new List<string>();

        foreach (var reference in _script!.Config.References)
        {
            if (reference is AssemblyReference aRef && aRef.AssemblyPath != null)
            {
                assemblyPaths.Add(aRef.AssemblyPath);
            }
            else if (reference is PackageReference pRef)
            {
                assemblyPaths.AddRange(
                    await _packageProvider.GetPackageAndDependanciesAssembliesAsync(pRef.PackageId, pRef.Version)
                );
            }
        }

        return assemblyPaths.ToArray();
    }

    public void AddOutputListener(IOutputWriter outputWriter)
    {
        _outputListeners.Add(outputWriter);
    }

    public void RemoveOutputListener(IOutputWriter outputWriter)
    {
        _outputListeners.Remove(outputWriter);
    }

    public void Dispose()
    {
        _logger.LogTrace("Dispose start");

        _outputListeners.Clear();
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

        _outputListeners.Clear();
        if (_serviceScope != null)
        {
            _serviceScope.Dispose();
            _serviceScope = null;
        }

        _logger.LogTrace("DisposeAsync end");
        return ValueTask.CompletedTask;
    }
}
