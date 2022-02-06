using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Compilation;
using NetPad.Exceptions;
using NetPad.Packages;
using NetPad.Runtimes.Assemblies;
using NetPad.Scripts;
using NetPad.Utilities;

namespace NetPad.Runtimes
{
    public sealed class ScriptRuntime : IScriptRuntime
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ICodeParser _codeParser;
        private readonly ICodeCompiler _codeCompiler;
        private readonly IPackageProvider _packageProvider;
        private readonly ILogger<ScriptRuntime> _logger;
        private Script? _script;

        public ScriptRuntime(
            IServiceProvider serviceProvider,
            ICodeParser codeParser,
            ICodeCompiler codeCompiler,
            IPackageProvider packageProvider,
            ILogger<ScriptRuntime> logger)
        {
            _serviceProvider = serviceProvider;
            _codeParser = codeParser;
            _codeCompiler = codeCompiler;
            _packageProvider = packageProvider;
            _logger = logger;
        }

        public Task InitializeAsync(Script script)
        {
            if (_script != null)
                throw new InvalidOperationException("Runtime is already initialized.");
            _script = script;
            return Task.CompletedTask;
        }

        public async Task<RunResult> RunAsync(IScriptRuntimeInputReader inputReader, IScriptRuntimeOutputWriter outputWriter)
        {
            EnsureInitialization();

            var script = _script!;
            var parsingResult = _codeParser.Parse(script!);

            try
            {
                var assemblyPaths = await GetReferenceAssemblyPathsAsync();

                var compilationResult = _codeCompiler.Compile(
                    new CompilationInput(parsingResult.Program, assemblyPaths)
                    {
                        OutputAssemblyNameTag = script.Name
                    });

                if (!compilationResult.Success)
                {
                    await outputWriter.WriteAsync(compilationResult.Diagnostics
                        .Where(d => d.Severity == DiagnosticSeverity.Error)
                        .JoinToString("\n") + "\n");
                    return RunResult.RunAttemptFailure();
                }

                var (alcWeakRef, completionSuccess, elapsedMs) = await ExecuteAndUnloadAsync(
                    compilationResult.AssemblyBytes,
                    assemblyPaths,
                    outputWriter
                );

                for (int i = 0; alcWeakRef.IsAlive && (i < 10); i++)
                {
                    GCUtil.CollectAndWait();
                }

                return !completionSuccess ? RunResult.ScriptCompletionFailure() : RunResult.Success(elapsedMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running script");
                await outputWriter.WriteAsync(ex + "\n");
                return RunResult.RunAttemptFailure();
            }
        }

        private void EnsureInitialization()
        {
            if (_script == null)
                throw new InvalidOperationException($"Script is not initialized.");
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
                    assemblyPaths.Add(
                        await _packageProvider.GetCachedPackageAssemblyPathAsync(pRef.PackageId, pRef.Version)
                    );
                }
            }

            return assemblyPaths.ToArray();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private async Task<(WeakReference alcWeakRef, bool completionSuccess, double elapsedMs)> ExecuteAndUnloadAsync(
            byte[] targetAssembly,
            string[] referenceAssemblyPaths,
            IScriptRuntimeOutputWriter outputWriter
        )
        {
            using var scope = _serviceProvider.CreateScope();
            using var assemblyLoader = new UnloadableAssemblyLoader(
                referenceAssemblyPaths,
                scope.ServiceProvider.GetRequiredService<ILogger<UnloadableAssemblyLoader>>()
            );

            var assembly = assemblyLoader.LoadFrom(targetAssembly);

            var alcWeakRef = new WeakReference(assemblyLoader, trackResurrection: true);

            var userScriptType = assembly.GetExportedTypes().FirstOrDefault(t => t.Name == "UserScript");
            if (userScriptType == null)
            {
                throw new ScriptRuntimeException("Could not find the UserScript type");
            }

            var method = userScriptType.GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new Exception("Could not find the entry method Main on UserScript");
            }

            var runStart = DateTime.Now;

            var task = method.Invoke(null, new object?[] { outputWriter }) as Task;

            if (task == null)
            {
                throw new ScriptRuntimeException("Expected a Task to be returned by executing the " +
                                                 $"script's Main method but got a {task?.GetType().FullName} ");
            }

            await task;
            var elapsedMs = (DateTime.Now - runStart).TotalMilliseconds;

            if (userScriptType.GetProperty("Exception", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) is Exception exception)
            {
                await outputWriter.WriteAsync(exception);
                return (alcWeakRef, false, elapsedMs);
            }

            return (alcWeakRef, true, elapsedMs);
        }

        public void Dispose()
        {
            _logger.LogTrace($"Dispose start");
            _logger.LogTrace($"Dispose end");
        }
    }
}
