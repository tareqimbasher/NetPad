using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Common;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.Events;
using NetPad.IO;
using NetPad.Runtimes;

namespace NetPad.Scripts;

public class ScriptEnvironment : IDisposable, IAsyncDisposable
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<ScriptEnvironment> _logger;
    private readonly IDataConnectionResourcesCache _dataConnectionResourcesCache;
    private IServiceScope _serviceScope;
    private IInputReader<string> _inputReader;
    private IOutputWriter<object> _outputWriter;
    private ScriptStatus _status;
    private Lazy<IScriptRuntime> _runtime;
    private bool _isDisposed;

    public ScriptEnvironment(Script script, IServiceScope serviceScope)
    {
        Script = script;
        _serviceScope = serviceScope;
        _eventBus = _serviceScope.ServiceProvider.GetRequiredService<IEventBus>();
        _dataConnectionResourcesCache = _serviceScope.ServiceProvider.GetRequiredService<IDataConnectionResourcesCache>();
        _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<ScriptEnvironment>>();
        _inputReader = ActionInputReader<string>.Null;
        _outputWriter = ActionOutputWriter<object>.Null;
        _status = ScriptStatus.Ready;

        // Forwards the following 2 property change notifications as messages on the event bus. They will eventually be pushed to IPC clients.
        Script.OnPropertyChanged.Add(async args =>
        {
            await _eventBus.PublishAsync(new ScriptPropertyChangedEvent(Script.Id, args.PropertyName, args.OldValue, args.NewValue));
        });

        Script.Config.OnPropertyChanged.Add(async args =>
        {
            await _eventBus.PublishAsync(new ScriptConfigPropertyChangedEvent(Script.Id, args.PropertyName, args.OldValue, args.NewValue));
        });

        _runtime = new Lazy<IScriptRuntime>(() =>
        {
            var factory = _serviceScope.ServiceProvider.GetRequiredService<IScriptRuntimeFactory>();
            var runtime = factory.CreateScriptRuntime(Script);
            _logger.LogDebug("Initialized new runtime");
            return runtime;
        });
    }

    public Script Script { get; }

    public virtual ScriptStatus Status => _status;

    public double RunDurationMilliseconds { get; private set; }

    public async Task RunAsync(RunOptions runOptions)
    {
        EnsureNotDisposed();

        _logger.LogTrace($"{nameof(RunAsync)} start");

        if (_status.NotIn(ScriptStatus.Ready, ScriptStatus.Error))
        {
            throw new InvalidOperationException($"Script is not in the correct state to run. Status is currently: {_status}");
        }

        await SetStatusAsync(ScriptStatus.Running);

        try
        {
            if (Script.DataConnection != null)
            {
                await AppendDataConnectionResourcesAsync(runOptions, Script.DataConnection);
            }

            // Script could have been requested to stop by this point
            if (Status == ScriptStatus.Stopping) return;

            if (Status == ScriptStatus.Stopping) return;

            var runResult = await _runtime.Value.RunScriptAsync(runOptions);

            await SetRunDurationAsync(runResult.DurationMs);

            await SetStatusAsync(runResult.IsScriptCompletedSuccessfully || runResult.IsRunCancelled ? ScriptStatus.Ready : ScriptStatus.Error);

            _logger.LogDebug("Run finished with status: {Status}", _status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running script");
            await _outputWriter.WriteAsync(new ErrorScriptOutput(ex));
            await SetStatusAsync(ScriptStatus.Error);
        }
        finally
        {
            _logger.LogTrace($"{nameof(RunAsync)} end");
        }
    }

    private async Task AppendDataConnectionResourcesAsync(RunOptions runOptions, DataConnection dataConnection)
    {
        var connectionCode = await _dataConnectionResourcesCache.GetSourceGeneratedCodeAsync(dataConnection, Script.Config.TargetFrameworkVersion);
        if (connectionCode.ApplicationCode.Any())
        {
            runOptions.AdditionalCode.AddRange(connectionCode.ApplicationCode);
        }

        var connectionAssembly = await _dataConnectionResourcesCache.GetAssemblyAsync(dataConnection, Script.Config.TargetFrameworkVersion);
        if (connectionAssembly != null)
        {
            runOptions.AdditionalReferences.Add(new AssemblyImageReference(connectionAssembly));
        }

        var requiredReferences = await _dataConnectionResourcesCache.GetRequiredReferencesAsync(dataConnection, Script.Config.TargetFrameworkVersion);

        if (requiredReferences.Any())
        {
            runOptions.AdditionalReferences.AddRange(requiredReferences);
        }
    }

    public async Task StopAsync()
    {
        EnsureNotDisposed();

        _logger.LogTrace($"{nameof(StopAsync)} start");

        if (Status != ScriptStatus.Running)
        {
            return;
        }

        var stopTime = DateTime.Now;
        await SetStatusAsync(ScriptStatus.Stopping);

        try
        {
            await _runtime.Value.StopScriptAsync();
            await _outputWriter.WriteAsync(new RawScriptOutput($"Script stopped at: {stopTime}"));
            await SetStatusAsync(ScriptStatus.Ready);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping script");
            await _outputWriter.WriteAsync(new ErrorScriptOutput(ex));
            await SetStatusAsync(ScriptStatus.Error);
        }
        finally
        {
            _logger.LogTrace($"{nameof(StopAsync)} end");
        }
    }

    public void SetIO(IInputReader<string> inputReader, IOutputWriter<object> outputWriter)
    {
        EnsureNotDisposed();

        RemoveScriptRuntimeIOHandlers();

        _inputReader = inputReader ?? throw new ArgumentNullException(nameof(inputReader));
        _outputWriter = outputWriter;

        AddScriptRuntimeIOHandlers();
    }

    public string[] GetScriptRuntimeUserAccessibleAssemblies() => _runtime.Value.GetUserAccessibleAssemblies();

    private async Task SetStatusAsync(ScriptStatus status)
    {
        if (status == _status)
        {
            return;
        }

        var oldValue = _status;
        _status = status;
        await _eventBus.PublishAsync(new EnvironmentPropertyChangedEvent(Script.Id, nameof(Status), oldValue, status));
    }

    private async Task SetRunDurationAsync(double runDurationMs)
    {
        var oldValue = RunDurationMilliseconds;
        RunDurationMilliseconds = runDurationMs;
        await _eventBus.PublishAsync(new EnvironmentPropertyChangedEvent(Script.Id, nameof(RunDurationMilliseconds), oldValue, runDurationMs));
    }

    private void AddScriptRuntimeIOHandlers()
    {
        _runtime.Value.AddInput(_inputReader);
        _runtime.Value.AddOutput(_outputWriter);
    }

    private void RemoveScriptRuntimeIOHandlers()
    {
        _runtime.Value.RemoveInput(_inputReader);
        _runtime.Value.RemoveOutput(_outputWriter);
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

        Dispose(false);
        GC.SuppressFinalize(this);

        _isDisposed = true;

        _logger.LogTrace($"{nameof(DisposeAsync)} end");
    }

    protected void Dispose(bool disposing)
    {
        if (disposing)
        {
            Try.Run(() => AsyncUtil.RunSync(async () => await StopAsync()));

            Script.RemoveAllPropertyChangedHandlers();
            Script.Config.RemoveAllPropertyChangedHandlers();

            _inputReader = ActionInputReader<string>.Null;
            _outputWriter = ActionOutputWriter<object>.Null;

            try
            {
                _runtime.Value.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing runtime of type: {ScriptRuntimeType}", _runtime.GetType().FullName);
            }

            _runtime = null!;

            _serviceScope.Dispose();
            _serviceScope = null!;
        }
    }

    protected async ValueTask DisposeAsyncCore()
    {
        await Try.RunAsync(async () => await StopAsync());

        Script.RemoveAllPropertyChangedHandlers();
        Script.Config.RemoveAllPropertyChangedHandlers();

        try
        {
            _runtime.Value.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing runtime of type: {RuntimeType}", _runtime.GetType().FullName);
        }

        _runtime = null!;

        if (_serviceScope is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        }
        else
        {
            _serviceScope.Dispose();
        }
    }
}
