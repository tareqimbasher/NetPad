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

        public Task RunAsync(IQueryRuntimeInputWriter inputReader, IQueryRuntimeOutputReader outputReader)
        {
            EnsureInitialization();
            HookConsole(inputReader, outputReader);

            string code = CodeParser.GetQueryCode(_query!);

            try
            {
                var compiler = new CodeCompiler();
                var assemblyBytes = compiler.Compile(new CompilationInput(code));

                var assembly = _assemblyLoader.LoadFrom(assemblyBytes);

                var type = assembly.GetExportedTypes().FirstOrDefault(t => t.Name == "NetPad_Query_Program");
                if (type == null)
                    throw new Exception("Could not find proper type");

                var program = Activator.CreateInstance(type);
                if (program == null)
                    throw new Exception("Could not create instance of type");

                var method = type.GetMethod("Main", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method == null)
                    throw new Exception("Could not find entry method");

                method.Invoke(program, Array.Empty<object?>());

                if (type.GetProperty("Exception")?.GetValue(program) is Exception exception)
                {
                    Console.WriteLine(exception);
                }

                // var modules = Process.GetCurrentProcess().Modules.Cast<ProcessModule>()
                //     .Where(x => x.ModuleName?.Contains("Hello") == true)
                //     .ToArray();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
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

        private void HookConsole(IQueryRuntimeInputWriter queryRuntimeInputReader, IQueryRuntimeOutputReader outputReader)
        {
            Console.SetOut(new QueryRuntimeOutputReaderTextWriter(outputReader));
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
