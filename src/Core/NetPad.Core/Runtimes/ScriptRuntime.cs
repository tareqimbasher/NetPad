using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NetPad.Scripts;
using NetPad.Runtimes.Assemblies;
using NetPad.Runtimes.Compilation;

namespace NetPad.Runtimes
{
    public sealed class ScriptRuntime : IScriptRuntime
    {
        private readonly IAssemblyLoader _assemblyLoader;
        private Script? _script;

        public ScriptRuntime(IAssemblyLoader assemblyLoader)
        {
            _assemblyLoader = assemblyLoader;
        }

        public Task InitializeAsync(Script script)
        {
            _script = script;
            return Task.CompletedTask;
        }

        public Task RunAsync(IScriptRuntimeInputReader inputReader, IScriptRuntimeOutputWriter outputWriter)
        {
            EnsureInitialization();

            string code = CodeParser.GetScriptCode(_script!);

            try
            {
                var compiler = new CodeCompiler();
                var assemblyBytes = compiler.Compile(new CompilationInput(code));

                var assembly = _assemblyLoader.LoadFrom(assemblyBytes);

                var userScriptType = assembly.GetExportedTypes().FirstOrDefault(t => t.Name == "UserScript");
                if (userScriptType == null)
                    throw new Exception("Could not find UserScript type");

                var method = userScriptType.GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic);
                if (method == null)
                    throw new Exception("Could not find entry method on UserScript");

                method.Invoke(null, new object?[] {outputWriter});

                if (userScriptType.GetProperty("Exception", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) is Exception exception)
                {
                    outputWriter.WriteAsync(exception);
                }

                // var modules = Process.GetCurrentProcess().Modules.Cast<ProcessModule>()
                //     .Where(x => x.ModuleName?.Contains("Hello") == true)
                //     .ToArray();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            // Task.Run(async () =>
            // {
            //     await Task.Delay(5000);
            //     var modules = Process.GetCurrentProcess().Modules.Cast<ProcessModule>()
            //         // .Where(x => x.ModuleName?.Contains("Hello") == true)
            //         .ToArray();
            // });

            return Task.CompletedTask;
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
