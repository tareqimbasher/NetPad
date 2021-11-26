using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Common;
using NetPad.Runtimes;

namespace NetPad.Scripts
{
    public sealed class ScriptEnvironment : INotifyOnPropertyChanged, IDisposable
    {
        private readonly IServiceScope _scope;
        private ScriptStatus _status;
        private int _runDurationMilliseconds;
        private IScriptRuntime? _runtime;
        private IScriptRuntimeInputReader _inputReader;
        private IScriptRuntimeOutputWriter _outputWriter;

        public ScriptEnvironment(Script script, IServiceScope scope)
        {
            _scope = scope;
            Script = script;
            _inputReader = ActionRuntimeInputReader.Default;
            _outputWriter = ActionRuntimeOutputWriter.Default;

            Status = ScriptStatus.Ready;
            OnPropertyChanged = new List<Func<PropertyChangedArgs, Task>>();
        }

        public Script Script { get; }

        public ScriptStatus Status
        {
            get => _status;
            set => this.RaiseAndSetIfChanged(ref _status, value);
        }

        public int RunDurationMilliseconds
        {
            get => _runDurationMilliseconds;
            set => this.RaiseAndSetIfChanged(ref _runDurationMilliseconds, value);
        }

        [JsonIgnore] public List<Func<PropertyChangedArgs, Task>> OnPropertyChanged { get; }
        public bool ShouldSerializeOnPropertyChanged() => false; // For Json.NET to ignore prop


        public async Task RunAsync()
        {
            Status = ScriptStatus.Running;

            try
            {
                if (_runtime == null)
                {
                    _runtime = _scope.ServiceProvider.GetRequiredService<IScriptRuntime>();
                    await _runtime.InitializeAsync(Script);
                }

                var start = DateTime.Now;

                var ranWithoutErrors = await _runtime.RunAsync(_inputReader, _outputWriter);

                RunDurationMilliseconds = (int)(DateTime.Now - start).TotalMilliseconds;
                Status = ranWithoutErrors ? ScriptStatus.Ready : ScriptStatus.Error;
            }
            catch (Exception ex)
            {
                await _outputWriter.WriteAsync(ex + "\n");
                Status = ScriptStatus.Error;
            }
        }

        public Task StopAsync()
        {
            return Task.CompletedTask;
        }

        public void SetIO(IScriptRuntimeInputReader inputReader, IScriptRuntimeOutputWriter outputWriter)
        {
            _inputReader = inputReader ?? throw new ArgumentNullException(nameof(inputReader));
            _outputWriter = outputWriter ?? throw new ArgumentNullException(nameof(outputWriter));
        }

        public void Dispose()
        {
            StopAsync();
            _scope.Dispose();
            this.RemoveAllPropertyChangedHandlers();
        }
    }
}
