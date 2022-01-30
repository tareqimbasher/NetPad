using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
                var assemblyPaths = new List<string>();
                foreach (var reference in script.Config.References)
                {
                    if (reference is AssemblyReference aRef && aRef.AssemblyPath != null)
                        assemblyPaths.Add(aRef.AssemblyPath);
                    else if (reference is PackageReference pRef)
                    {
                        assemblyPaths.Add(
                            await _packageProvider.GetCachedPackageAssemblyPathAsync(pRef.PackageId, pRef.Version)
                        );
                    }
                }

                var compilationResult = _codeCompiler.Compile(new CompilationInput(
                    parsingResult.Program,
                    assemblyPaths)
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

                using var scope = _serviceProvider.CreateScope();
                using var loader = new UnloadableAssemblyLoader(
                    assemblyPaths.Select(p => (AssemblyName.GetAssemblyName(p).FullName, File.ReadAllBytes(p))),
                    scope.ServiceProvider.GetRequiredService<ILogger<UnloadableAssemblyLoader>>()
                );

                var assembly = loader.LoadFrom(compilationResult.AssemblyBytes);

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

                if (userScriptType.GetProperty("Exception", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) is Exception exception)
                {
                    await outputWriter.WriteAsync(exception);
                    return RunResult.ScriptCompletionFailure();
                }

                return RunResult.Success((DateTime.Now - runStart).TotalMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error running script: Details: {ex}");
                await outputWriter.WriteAsync(ex + "\n");
                return RunResult.RunAttemptFailure();
            }
        }

        private void EnsureInitialization()
        {
            if (_script == null)
                throw new InvalidOperationException($"Script is not initialized.");
        }

        public void Dispose()
        {
            _logger.LogTrace($"Dispose start");
            _logger.LogTrace($"Dispose end");
        }
    }
}
