using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using NetPad.Compilation;
using NetPad.Extensions;
using NetPad.Scripts;
using NetPad.Runtimes.Assemblies;

namespace NetPad.Runtimes
{
    public sealed class ScriptRuntime : IScriptRuntime
    {
        private readonly IAssemblyLoader _assemblyLoader;
        private readonly ICodeParser _codeParser;
        private readonly ICodeCompiler _codeCompiler;
        private Script? _script;

        public ScriptRuntime(IAssemblyLoader assemblyLoader, ICodeParser codeParser, ICodeCompiler codeCompiler)
        {
            _assemblyLoader = assemblyLoader;
            _codeParser = codeParser;
            _codeCompiler = codeCompiler;
        }

        public Task InitializeAsync(Script script)
        {
            _script = script;
            return Task.CompletedTask;
        }

        public async Task<bool> RunAsync(IScriptRuntimeInputReader inputReader, IScriptRuntimeOutputWriter outputWriter)
        {
            EnsureInitialization();

            var result = _codeParser.Parse(_script!);

            try
            {
                var compilationResult = _codeCompiler.Compile(new CompilationInput(result.Program));

                if (!compilationResult.Success)
                {
                    await outputWriter.WriteAsync(compilationResult.Diagnostics
                        .Where(d => d.Severity == DiagnosticSeverity.Error)
                        .JoinToString("\n") + "\n");
                    return false;
                }

                var assembly = _assemblyLoader.LoadFrom(compilationResult.AssemblyBytes);

                var userScriptType = assembly.GetExportedTypes().FirstOrDefault(t => t.Name == "UserScript");
                if (userScriptType == null)
                    throw new Exception("Could not find UserScript type");

                var method = userScriptType.GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic);
                if (method == null)
                    throw new Exception("Could not find entry method on UserScript");

                var task = method.Invoke(null, new object?[] { outputWriter }) as Task;

                await task;

                if (userScriptType.GetProperty("Exception", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) is Exception exception)
                {
                    await outputWriter.WriteAsync(exception);
                    return false;
                }

                // var modules = Process.GetCurrentProcess().Modules.Cast<ProcessModule>()
                //     .Where(x => x.ModuleName?.Contains("Hello") == true)
                //     .ToArray();
            }
            catch (Exception ex)
            {
                await outputWriter.WriteAsync(ex + "\n");
                return false;
            }

            return true;

            // Task.Run(async () =>
            // {
            //     await Task.Delay(5000);
            //     var modules = Process.GetCurrentProcess().Modules.Cast<ProcessModule>()
            //         // .Where(x => x.ModuleName?.Contains("Hello") == true)
            //         .ToArray();
            // });
        }

        private void EnsureInitialization()
        {
            if (_script == null)
                throw new InvalidOperationException($"Script is not initialized.");
        }

        public void Dispose()
        {
            _assemblyLoader.UnloadLoadedAssemblies();
        }
    }
}
