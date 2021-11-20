using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using NetPad.Queries;
using NetPad.Runtimes.Assemblies;
using NetPad.Runtimes.Compilation;

namespace NetPad.Runtimes
{
    public sealed class QueryRuntime : IQueryRuntime
    {
        private readonly IAssemblyLoader _assemblyLoader;
        private Query? _query;

        public QueryRuntime(IAssemblyLoader assemblyLoader)
        {
            _assemblyLoader = assemblyLoader;
        }

        public Task InitializeAsync(Query query)
        {
            _query = query;
            return Task.CompletedTask;
        }

        public Task RunAsync(IQueryRuntimeInputReader inputReader, IQueryRuntimeOutputWriter outputWriter)
        {
            EnsureInitialization();

            string code = CodeParser.GetQueryCode(_query!);

            try
            {
                var compiler = new CodeCompiler();
                var assemblyBytes = compiler.Compile(new CompilationInput(code));

                var assembly = _assemblyLoader.LoadFrom(assemblyBytes);

                var userQueryType = assembly.GetExportedTypes().FirstOrDefault(t => t.Name == "UserQuery");
                if (userQueryType == null)
                    throw new Exception("Could not find UserQuery type");

                var method = userQueryType.GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic);
                if (method == null)
                    throw new Exception("Could not find entry method on UserQuery");

                method.Invoke(null, new object?[] {outputWriter});

                if (userQueryType.GetProperty("Exception", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) is Exception exception)
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
            if (_query == null)
                throw new InvalidOperationException($"Query is not initialized.");
        }

        public void Dispose()
        {
            _assemblyLoader.UnloadLoadedAssemblies();
        }
    }
}
