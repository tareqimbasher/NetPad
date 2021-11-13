using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NetPad.Queries;
using NetPad.Runtimes;
using NetPad.Runtimes.Compilation;

namespace NetPad.Runtimes
{
    public sealed class MainAppDomainQueryRuntime : IQueryRuntime
    {
        private Query? _query;

        public async Task InitializeAsync(Query query)
        {
            _query = query;
        }

        public async Task RunAsync(IQueryRuntimeInputWriter inputReader, IQueryRuntimeOutputReader outputReader)
        {
            EnsureInitialization();
            HookConsole(inputReader, outputReader);

            string code = CodeParser.GetQueryCode(_query!);

            try
            {
                var compiler = new CodeCompiler();
                var assemblyBytes = compiler.Compile(new CompilationInput(code));
                var assembly = AppDomain.CurrentDomain.Load(assemblyBytes);

                var type = assembly.GetExportedTypes().FirstOrDefault(t => t.Name == "NetPad_Query_Program");
                if (type == null)
                    throw new Exception("Could not find proper type");

                var @object = Activator.CreateInstance(type);
                if (@object == null)
                    throw new Exception("Could not create instance of type");

                var method = type.GetMethod("Main", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method == null)
                    throw new Exception("Could not find entry method");

                method.Invoke(@object, Array.Empty<object?>());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void HookConsole(IQueryRuntimeInputWriter queryRuntimeInputReader, IQueryRuntimeOutputReader outputReader)
        {
            Console.SetOut(new MemoryTextWriter(outputReader));
        }

        private void EnsureInitialization()
        {
            if (_query == null)
                throw new InvalidOperationException($"Query is not initialized.");
        }
    }
}

public class MemoryTextWriter : TextWriter
{
    private readonly IQueryRuntimeOutputReader _outputReader;

    public override Encoding Encoding => Encoding.Default;

    public MemoryTextWriter(IQueryRuntimeOutputReader outputReader)
    {
        _outputReader = outputReader;
    }

    public override void Write(string? value)
    {
        _outputReader.ReadAsync(value);
    }

    public override void WriteLine()
    {
        _outputReader.ReadAsync("\n");
    }
}
