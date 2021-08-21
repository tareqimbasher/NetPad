using System;
using System.Threading.Tasks;
using NetPad.Queries;
using NetPad.Runtimes.Compilation;

namespace NetPad.Runtimes
{
    public class AppDomainQueryRuntime : IQueryRuntime
    {
        private Query? _query;

        public AppDomainQueryRuntime()
        {
        }

        public async Task InitializeAsync(Query query)
        {
            _query = query;
        }

        public async Task RunAsync(IQueryRuntimeInputReader inputReader, IQueryRuntimeOutputReader outputReader)
        {
            EnsureInitialization();

            var code = _query!.Code;
            var appDomain = new QueryAppDomain();

            var compiler = new CodeCompiler();
            var assembly = compiler.Compile(code);

            appDomain.AppDomain.Load(assembly);
        }

        private void EnsureInitialization()
        {
            if (_query == null)
                throw new InvalidOperationException($"Query runtime is not initialized.");
        }
    }
}