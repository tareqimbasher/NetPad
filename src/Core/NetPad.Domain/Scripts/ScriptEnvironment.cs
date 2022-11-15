using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Common;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.Events;
using NetPad.IO;
using NetPad.Runtimes;

namespace NetPad.Scripts
{
    // If this class is sealed, IDisposable and IAsyncDisposable implementations should be revised
    public class ScriptEnvironment : IDisposable, IAsyncDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly ILogger<ScriptEnvironment> _logger;
        private readonly IDataConnectionResourcesCache _dataConnectionResourcesCache;
        private IServiceScope? _serviceScope;
        private IInputReader _inputReader;
        private IScriptOutput _output;
        private ScriptStatus _status;
        private double _runDurationMilliseconds;
        private IScriptRuntime? _runtime;
        private bool _isDisposed;

        public ScriptEnvironment(Script script, IServiceScope serviceScope)
        {
            Script = script;
            _serviceScope = serviceScope;
            _eventBus = _serviceScope.ServiceProvider.GetRequiredService<IEventBus>();
            _dataConnectionResourcesCache = _serviceScope.ServiceProvider.GetRequiredService<IDataConnectionResourcesCache>();
            _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<ScriptEnvironment>>();
            _inputReader = ActionInputReader.Null;
            _output = IScriptOutput.Null;
            _status = ScriptStatus.Ready;

            Initialize();
        }

        public Script Script { get; }

        public virtual ScriptStatus Status => _status;

        public double RunDurationMilliseconds => _runDurationMilliseconds;

        public async Task RunAsync(RunOptions runOptions)
        {
            EnsureNotDisposed();

            _logger.LogTrace($"{nameof(RunAsync)} start");

            if (Status == ScriptStatus.Running)
                throw new InvalidOperationException("Script is already running.");

            await SetStatusAsync(ScriptStatus.Running);

            try
            {
                if (Script.DataConnection != null)
                {
                    var connectionCode = await _dataConnectionResourcesCache.GetSourceGeneratedCodeAsync(Script.DataConnection);
                    if (connectionCode.ApplicationCode.Any())
                    {
                        runOptions.AdditionalCode.AddRange(connectionCode.ApplicationCode);
                    }

                    var connectionAssembly = await _dataConnectionResourcesCache.GetAssemblyAsync(Script.DataConnection);
                    if (connectionAssembly != null)
                    {
                        runOptions.AdditionalReferences.Add(new AssemblyImageReference(connectionAssembly));
                    }

                    var requiredReferences = await _dataConnectionResourcesCache.GetRequiredReferencesAsync(Script.DataConnection);
                    if (requiredReferences.Any())
                    {
                        runOptions.AdditionalReferences.AddRange(requiredReferences);
                    }
                }

                var runtime = await GetRuntimeAsync();
                var runResult = await runtime.RunScriptAsync(runOptions);

                await SetRunDurationAsync(runResult.DurationMs);
                await SetStatusAsync(runResult.IsScriptCompletedSuccessfully ? ScriptStatus.Ready : ScriptStatus.Error);

                _logger.LogDebug("Run completed with status: {Status}", Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running script");
                await _output.PrimaryChannel.WriteAsync(ex + "\n");
                await SetStatusAsync(ScriptStatus.Error);
            }
            finally
            {
                _logger.LogTrace($"{nameof(RunAsync)} end");
            }
        }

        public void SetIO(IInputReader inputReader, IScriptOutput output)
        {
            EnsureNotDisposed();

            RemoveScriptRuntimeListeners();

            _inputReader = inputReader ?? throw new ArgumentNullException(nameof(inputReader));
            _output = output ?? throw new ArgumentNullException(nameof(output));

            AddScriptRuntimeListeners();
        }

        private void Initialize()
        {
            EnsureNotDisposed();

            Script.OnPropertyChanged.Add(async (args) =>
            {
                await _eventBus.PublishAsync(new ScriptPropertyChangedEvent(Script.Id, args.PropertyName, args.NewValue));
            });

            Script.Config.OnPropertyChanged.Add(async (args) =>
            {
                await _eventBus.PublishAsync(new ScriptConfigPropertyChangedEvent(Script.Id, args.PropertyName, args.NewValue));
            });
        }

        private async Task SetStatusAsync(ScriptStatus status)
        {
            _status = status;
            await _eventBus.PublishAsync(new EnvironmentPropertyChangedEvent(Script.Id, nameof(Status), status));
        }

        private async Task SetRunDurationAsync(double runDurationMs)
        {
            _runDurationMilliseconds = runDurationMs;
            await _eventBus.PublishAsync(new EnvironmentPropertyChangedEvent(Script.Id, nameof(RunDurationMilliseconds), runDurationMs));
        }

        private async Task<IScriptRuntime> GetRuntimeAsync()
        {
            if (_runtime == null)
            {
                _logger.LogDebug("Initializing new runtime");

                var factory = _serviceScope!.ServiceProvider.GetRequiredService<IScriptRuntimeFactory>();
                _runtime = await factory.CreateScriptRuntimeAsync(Script);

                AddScriptRuntimeListeners();
            }

            return _runtime;
        }

        private void AddScriptRuntimeListeners()
        {
            _runtime?.AddOutput(_output);
        }

        private void RemoveScriptRuntimeListeners()
        {
            _runtime?.RemoveOutput(_output);
        }

        private void EnsureNotDisposed()
        {
            if (_isDisposed)
                throw new InvalidOperationException($"Script environment {Script.Id} is disposed.");
        }

        public void Dispose()
        {
            _logger.LogTrace($"{nameof(Dispose)} start");

            Dispose(true);
            GC.SuppressFinalize(this);

            _isDisposed = true;

            _logger.LogTrace($"{nameof(Dispose)} end");
        }

        public async ValueTask DisposeAsync()
        {
            _logger.LogTrace($"{nameof(DisposeAsync)} start");

            await DisposeAsyncCore().ConfigureAwait(false);

            Dispose(disposing: false);
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
            GC.SuppressFinalize(this);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize

            _isDisposed = true;

            _logger.LogTrace($"{nameof(DisposeAsync)} end");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _runtime?.Dispose();
                _serviceScope?.Dispose();

                _runtime = null;
                _serviceScope = null;

                Script.RemoveAllPropertyChangedHandlers();
                Script.Config.RemoveAllPropertyChangedHandlers();
            }
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (_runtime != null)
            {
                await _runtime.DisposeAsync().ConfigureAwait(false);
            }

            if (_serviceScope is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            }
            else
            {
                _serviceScope?.Dispose();
            }

            _runtime = null;
            _serviceScope = null;
        }
    }
}
