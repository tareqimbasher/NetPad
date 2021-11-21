using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Runtimes;

namespace NetPad.Scripts
{
    public class ScriptEnvironment : IDisposable
    {
        private readonly IServiceScope _scope;

        public ScriptEnvironment(Script script, IServiceScope scope)
        {
            _scope = scope;
            Script = script;
            Status = ScriptStatus.Ready;
        }

        public Script Script { get; }

        public ScriptStatus Status { get; private set; }

        public virtual async Task RunAsync(IScriptRuntimeInputReader? inputReader = null, IScriptRuntimeOutputWriter? outputWriter = null)
        {
            Status = ScriptStatus.Running;

            outputWriter ??= new ActionRuntimeOutputWriter(o => { /* Do nothing */ });

            try
            {
                var runtime = _scope.ServiceProvider.GetRequiredService<IScriptRuntime>();
                await runtime.InitializeAsync(Script);

                await runtime.RunAsync(inputReader, outputWriter);
            }
            finally
            {
                Status = ScriptStatus.Ready;
            }
        }

        public virtual async Task StopAsync()
        {
            throw new NotImplementedException();
        }

        public virtual async Task CloseAsync()
        {

        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _scope.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
