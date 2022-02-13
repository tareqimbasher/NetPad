using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Common;
using NetPad.Events;
using NetPad.Runtimes;

namespace NetPad.Scripts
{
    public class ScriptEnvironment : IDisposable
    {
        private readonly IServiceScope _scope;
        private readonly IEventBus _eventBus;
        private readonly ILogger<ScriptEnvironment> _logger;
        private IScriptRuntimeInputReader _inputReader;
        private IScriptRuntimeOutputWriter _outputWriter;

        private ScriptStatus _status;
        private double _runDurationMilliseconds;
        private IScriptRuntime? _runtime;
        private readonly object _destructionLock = new object();
        private bool _isDestroyed = false;


        public ScriptEnvironment(Script script, IServiceScope scope)
        {
            Script = script;
            _scope = scope;
            _eventBus = _scope.ServiceProvider.GetRequiredService<IEventBus>();
            _logger = _scope.ServiceProvider.GetRequiredService<ILogger<ScriptEnvironment>>();
            _inputReader = ActionRuntimeInputReader.Null;
            _outputWriter = ActionRuntimeOutputWriter.Null;

            _status = ScriptStatus.Ready;

            Initialize();
        }

        public Script Script { get; }

        public virtual ScriptStatus Status => _status;

        public double RunDurationMilliseconds => _runDurationMilliseconds;

        public async Task RunAsync()
        {
            _logger.LogTrace($"{nameof(RunAsync)} start");

            EnsureNotDestroyed();

            if (Status == ScriptStatus.Running)
                throw new InvalidOperationException("Script is already running.");

            await SetStatusAsync(ScriptStatus.Running);

            try
            {
                var runtime = await GetRuntimeAsync();

                var runResult = await runtime.RunAsync(_inputReader, _outputWriter);

                await SetRunDurationAsync(runResult.DurationMs);
                await SetStatusAsync(runResult.IsScriptCompletedSuccessfully ? ScriptStatus.Ready : ScriptStatus.Error);

                _logger.LogDebug($"Run completed with status: {Status}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error running script. Details: {ex}");
                await _outputWriter.WriteAsync(ex + "\n");
                await SetStatusAsync(ScriptStatus.Error);
            }
            finally
            {
                _logger.LogTrace($"{nameof(RunAsync)} end");
            }
        }

        public virtual Task DestroyAsync()
        {
            lock (_destructionLock)
            {
                EnsureNotDestroyed();
                _scope.Dispose();

                Script.RemoveAllPropertyChangedHandlers();
                Script.Config.RemoveAllPropertyChangedHandlers();

                _isDestroyed = true;
            }

            return Task.CompletedTask;
        }

        public void SetIO(IScriptRuntimeInputReader inputReader, IScriptRuntimeOutputWriter outputWriter)
        {
            _inputReader = inputReader ?? throw new ArgumentNullException(nameof(inputReader));
            _outputWriter = outputWriter ?? throw new ArgumentNullException(nameof(outputWriter));
        }

        private void Initialize()
        {
            if (Script.IsNew)
            {
                Script.Config.SetNamespaces(ScriptConfigDefaults.DefaultNamespaces);
            }

            Script.OnPropertyChanged.Add(async (args) =>
            {
                await _eventBus.PublishAsync(new ScriptPropertyChanged(Script.Id, args.PropertyName, args.NewValue));
            });

            Script.Config.OnPropertyChanged.Add(async (args) =>
            {
                await _eventBus.PublishAsync(new ScriptConfigPropertyChanged(Script.Id, args.PropertyName, args.NewValue));
            });
        }

        private async Task SetStatusAsync(ScriptStatus status)
        {
            _status = status;
            await _eventBus.PublishAsync(new EnvironmentPropertyChanged(Script.Id, nameof(Status), status));
        }

        private async Task SetRunDurationAsync(double runDurationMs)
        {
            _runDurationMilliseconds = runDurationMs;
            await _eventBus.PublishAsync(new EnvironmentPropertyChanged(Script.Id, nameof(RunDurationMilliseconds), runDurationMs));
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

        private void EnsureNotDestroyed()
        {
            if (_isDestroyed)
                throw new InvalidOperationException($"{nameof(ScriptEnvironment)} is destroyed.");
        }

        public void Dispose()
        {
            _logger.LogTrace($"{nameof(Dispose)} start");
            DestroyAsync();
            _logger.LogTrace($"{nameof(Dispose)} end");
        }
    }
}
