using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Common;
using NetPad.Exceptions;
using NetPad.Runtimes;

namespace NetPad.Scripts
{
    public class ScriptEnvironment : INotifyOnPropertyChanged, IDisposable
    {
        private readonly IServiceScope _scope;
        private ScriptStatus _status;
        private IScriptRuntimeInputReader? _inputReader;
        private IScriptRuntimeOutputWriter? _outputWriter;

        public ScriptEnvironment(Script script, IServiceScope scope)
        {
            _scope = scope;
            Script = script;
            Status = ScriptStatus.Ready;
            OnPropertyChanged = new List<Func<PropertyChangedArgs, Task>>();
        }

        public Script Script { get; }

        public ScriptStatus Status
        {
            get => _status;
            set => this.RaiseAndSetIfChanged(ref _status, value);
        }

        [JsonIgnore]
        public List<Func<PropertyChangedArgs, Task>> OnPropertyChanged { get; }

        public virtual async Task RunAsync()
        {
            Status = ScriptStatus.Running;

            if (_outputWriter == null)
                _outputWriter = new ActionRuntimeOutputWriter(o => { /* Do nothing */ });

            try
            {
                var runtime = _scope.ServiceProvider.GetRequiredService<IScriptRuntime>();
                await runtime.InitializeAsync(Script);

                await runtime.RunAsync(_inputReader, _outputWriter);
            }
            catch (CodeCompilationException ex)
            {
                await _outputWriter.WriteAsync(ex.ErrorsAsString() + "\n");
            }
            catch (Exception ex)
            {
                await _outputWriter.WriteAsync(ex + "\n");
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

        public void SetIO(IScriptRuntimeInputReader? inputReader = null, IScriptRuntimeOutputWriter? outputWriter = null)
        {
            _inputReader = inputReader;
            _outputWriter = outputWriter;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _scope.Dispose();
                this.RemoveAllPropertyChangedHandlers();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
