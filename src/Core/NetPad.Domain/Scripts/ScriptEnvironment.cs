using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Common;
using NetPad.Runtimes;

namespace NetPad.Scripts
{
    public sealed class ScriptEnvironment : INotifyOnPropertyChanged, IDisposable
    {
        private readonly IServiceScope _scope;
        private readonly ILogger<ScriptEnvironment> _logger;
        private ScriptStatus _status;
        private double _runDurationMilliseconds;
        private IScriptRuntime? _runtime;
        private IScriptRuntimeInputReader _inputReader;
        private IScriptRuntimeOutputWriter _outputWriter;

        public ScriptEnvironment(Script script, IServiceScope scope)
        {
            Script = script;
            _scope = scope;
            _logger = _scope.ServiceProvider.GetRequiredService<ILogger<ScriptEnvironment>>();
            _inputReader = ActionRuntimeInputReader.Null;
            _outputWriter = ActionRuntimeOutputWriter.Null;

            Status = ScriptStatus.Ready;
            OnPropertyChanged = new List<Func<PropertyChangedArgs, Task>>();

            if (script.IsNew)
            {
                script.Config.SetNamespaces(ScriptConfigDefaults.DefaultNamespaces);
            }
        }

        public Script Script { get; }

        public ScriptStatus Status
        {
            get => _status;
            set => this.RaiseAndSetIfChanged(ref _status, value);
        }

        public double RunDurationMilliseconds
        {
            get => _runDurationMilliseconds;
            set => this.RaiseAndSetIfChanged(ref _runDurationMilliseconds, value);
        }

        [JsonIgnore] public List<Func<PropertyChangedArgs, Task>> OnPropertyChanged { get; }

        public async Task RunAsync()
        {
            _logger.LogTrace($"{nameof(RunAsync)} start");

            Status = ScriptStatus.Running;

            try
            {
                var runtime = await GetRuntimeAsync();

                var runResult = await runtime.RunAsync(_inputReader, _outputWriter);

                RunDurationMilliseconds = runResult.DurationMs;
                Status = runResult.IsScriptCompletedSuccessfully ? ScriptStatus.Ready : ScriptStatus.Error;
                _logger.LogDebug($"Run completed with status: {Status}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error running script. Details: {ex}");
                await _outputWriter.WriteAsync(ex + "\n");
                Status = ScriptStatus.Error;
            }
            finally
            {
                _logger.LogTrace($"{nameof(RunAsync)} end");
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

        private async Task<IScriptRuntime> GetRuntimeAsync()
        {
            if (_runtime == null)
            {
                _logger.LogDebug($"Initializing new runtime");
                _runtime = _scope.ServiceProvider.GetRequiredService<IScriptRuntime>();
                await _runtime.InitializeAsync(Script);
            }

            return _runtime;
        }

        public void Dispose()
        {
            _logger.LogTrace($"{nameof(Dispose)} start");
            StopAsync();
            this.RemoveAllPropertyChangedHandlers();
            _scope.Dispose();
            _logger.LogTrace($"{nameof(Dispose)} end");
        }
    }
}
