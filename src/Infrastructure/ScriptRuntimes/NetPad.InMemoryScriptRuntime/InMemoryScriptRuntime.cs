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

namespace NetPad.Runtimes
{
    // If this class is unsealed, IDisposable and IAsyncDisposable implementations must be revised
    public sealed class InMemoryScriptRuntime : IScriptRuntime
    {
        private readonly Script _script;
        private readonly ICodeParser _codeParser;
        private readonly ICodeCompiler _codeCompiler;
        private readonly IPackageProvider _packageProvider;
        private readonly ILogger<InMemoryScriptRuntime> _logger;
        private readonly IOutputWriter _outputWriter;
        private readonly HashSet<IOutputWriter> _outputListeners;
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
            try
            {
                var (success, assemblyBytes, referenceAssemblyPaths, parsingResult) = await CompileAndGetRefAssemblyPathsAsync(runOptions);

                if (!success)
                    return RunResult.RunAttemptFailure();

                var (alcWeakRef, completionSuccess, elapsedMs) = await ExecuteInMemoryAndUnloadAsync(
                    _serviceScope!,
                    assemblyBytes,
                    referenceAssemblyPaths,
                    parsingResult.ParsedCodeInformation,
                    _outputWriter
                );

                for (int i = 0; alcWeakRef.IsAlive && (i < 10); i++)
                {
                    GCUtil.CollectAndWait();
                }

                return !completionSuccess ? RunResult.ScriptCompletionFailure(elapsedMs) : RunResult.Success(elapsedMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running script");
                await _outputWriter.WriteAsync(ex + "\n");
                return RunResult.RunAttemptFailure();
            }
        }

        public void AddOutputListener(IOutputWriter outputWriter)
        {
            _outputListeners.Add(outputWriter);
        }

        public void RemoveOutputListener(IOutputWriter outputWriter)
        {
            _outputListeners.Remove(outputWriter);
        }

        private async Task<(bool success, byte[] assemblyBytes, string[] referenceAssemblyPaths, CodeParsingResult parsingResult)>
            CompileAndGetRefAssemblyPathsAsync(RunOptions runOptions)
        {
            var parsingResult = _codeParser.Parse(_script, new CodeParsingOptions
            {
                IncludedCode = runOptions.SpecificCodeToRun,
                AdditionalCode = runOptions.AdditionalCode
            });

            var referenceAssemblyPaths = await GetReferenceAssemblyPathsAsync(
                _script.Config.References.Union(runOptions.AdditionalReferences)
            );

            var fullProgram = parsingResult.GetFullProgram()
                .Replace("Console.WriteLine", $"{parsingResult.ParsedCodeInformation.BootstrapperClassName}.OutputWriteLine")
                .Replace("Console.Write", $"{parsingResult.ParsedCodeInformation.BootstrapperClassName}.OutputWrite");

            var compilationResult = _codeCompiler.Compile(
                new CompilationInput(fullProgram, referenceAssemblyPaths).WithOutputAssemblyNameTag(_script.Name));

            if (!compilationResult.Success)
            {
                await _outputWriter.WriteAsync(compilationResult.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .JoinToString("\n") + "\n");

                return (false, Array.Empty<byte>(), Array.Empty<string>(), parsingResult);
            }

            return (true, compilationResult.AssemblyBytes, referenceAssemblyPaths, parsingResult);
        }

        private async Task<string[]> GetReferenceAssemblyPathsAsync(IEnumerable<Reference> references)
        {
            var assemblyPaths = new List<string>();

            foreach (var reference in references.Distinct())
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

            // return assemblyPaths
            //     .Distinct()

            return assemblyPaths.ToArray();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private async Task<(WeakReference alcWeakRef, bool completionSuccess, double elapsedMs)> ExecuteInMemoryAndUnloadAsync(
            IServiceScope serviceScope,
            byte[] targetAssembly,
            string[] referenceAssemblyPaths,
            ParsedCodeInformation parsedCodeInformation,
            IOutputWriter outputWriter
        )
        {
            using var scope = serviceScope.ServiceProvider.CreateScope();
            using var assemblyLoader = new UnloadableAssemblyLoader(
                referenceAssemblyPaths,
                scope.ServiceProvider.GetRequiredService<ILogger<UnloadableAssemblyLoader>>()
            );

            var assembly = assemblyLoader.LoadFrom(targetAssembly);

            var alcWeakRef = new WeakReference(assemblyLoader, trackResurrection: true);

            string bootstrapperClassName = parsedCodeInformation.BootstrapperClassName;
            Type? bootstrapperType = assembly.GetTypes().FirstOrDefault(t => t.Name == bootstrapperClassName);
            if (bootstrapperType == null)
            {
                throw new ScriptRuntimeException($"Could not find the bootstrapper type: {bootstrapperClassName}");
            }

            string setIOMethodName = parsedCodeInformation.BootstrapperSetIOMethodName;
            MethodInfo? setIOMethod = bootstrapperType.GetMethod(setIOMethodName, BindingFlags.Static | BindingFlags.NonPublic);
            if (setIOMethod == null)
            {
                throw new Exception($"Could not find the entry method {setIOMethodName} on bootstrapper type: {bootstrapperClassName}");
            }

            setIOMethod.Invoke(null, new object?[] { outputWriter });

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
                await outputWriter.WriteAsync((ex.InnerException ?? ex).ToString());
                return (alcWeakRef, false, GetElapsedMilliseconds(runStart));
            }

            return (alcWeakRef, true, GetElapsedMilliseconds(runStart));
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

        private double GetElapsedMilliseconds(DateTime start)
        {
            return (DateTime.Now - start).TotalMilliseconds;
        }
    }
}
