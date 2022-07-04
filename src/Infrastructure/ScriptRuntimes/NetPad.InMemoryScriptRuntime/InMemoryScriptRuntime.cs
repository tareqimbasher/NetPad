using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Assemblies;
using NetPad.Compilation;
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

        public async Task<RunResult> RunScriptAsync()
        {
            try
            {
                var (success, assemblyBytes, referenceAssemblyPaths) = await CompileAndGetRefAssemblyPathsAsync();

                if (!success)
                    return RunResult.RunAttemptFailure();

                var (alcWeakRef, completionSuccess, elapsedMs) = await ExecuteInMemoryAndUnloadAsync(
                    _serviceScope!,
                    assemblyBytes,
                    referenceAssemblyPaths,
                    _outputWriter
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

        private async Task<(bool success, byte[] assemblyBytes, string[] referenceAssemblyPaths)> CompileAndGetRefAssemblyPathsAsync()
        {
            var parsingResult = _codeParser.Parse(_script);

            var referenceAssemblyPaths = await GetReferenceAssemblyPathsAsync();

            var fullProgram = parsingResult.FullProgram
                .Replace("Console.WriteLine", "Program.OutputWriteLine")
                .Replace("Console.Write", "Program.OutputWrite");

            var compilationResult = _codeCompiler.Compile(
                new CompilationInput(fullProgram, referenceAssemblyPaths)
                {
                    OutputAssemblyNameTag = _script.Name
                });

            if (!compilationResult.Success)
            {
                await _outputWriter.WriteAsync(compilationResult.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(e =>
                    {
                        // TODO fix REAAAAALLY janky way of correcting line numbers
                        try
                        {
                            var message = e.ToString();
                            if (!message.StartsWith("("))
                                return message;

                            var parts = message.Split(')');

                            var part1 = parts[0].TrimStart('(')
                                .Split(',')
                                .Select(x => int.Parse(x.Trim()))
                                .ToArray();

                            return $"({part1[0] - 69},{part1[1]})" + string.Join(")", parts.Skip(1));
                        }
                        catch
                        {
                            return e.ToString();
                        }

                    })
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private async Task<(WeakReference alcWeakRef, bool completionSuccess, double elapsedMs)> ExecuteInMemoryAndUnloadAsync(
            IServiceScope serviceScope,
            byte[] targetAssembly,
            string[] referenceAssemblyPaths,
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

            var userScriptType = assembly.GetExportedTypes().FirstOrDefault(t => t.Name == "Program");
            if (userScriptType == null)
            {
                throw new ScriptRuntimeException("Could not find the Program type");
            }

            var method = userScriptType.GetMethod("Start", BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new Exception("Could not find the entry method Start on Program");
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
}
