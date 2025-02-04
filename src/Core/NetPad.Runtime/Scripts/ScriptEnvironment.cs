using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Common;
using NetPad.Events;
using NetPad.ExecutionModel;
using NetPad.ExecutionModel.ClientServer.ScriptServices;
using NetPad.IO;
using NetPad.Presentation;
using NetPad.Scripts.Events;

namespace NetPad.Scripts;

public class ScriptEnvironment : IDisposable, IAsyncDisposable
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<ScriptEnvironment> _logger;
    private IServiceScope _serviceScope;
    private IInputReader<string> _inputReader;
    private IOutputWriter<object> _outputWriter;
    private Lazy<IScriptRunner> _runner;
    private readonly List<IDisposable> _disposables = new();
    private bool _isDisposed;

    public ScriptEnvironment(Script script, IServiceScope serviceScope)
    {
        Script = script;
        Status = ScriptStatus.Ready;
        _serviceScope = serviceScope;
        _eventBus = _serviceScope.ServiceProvider.GetRequiredService<IEventBus>();
        _logger = _serviceScope.ServiceProvider.GetRequiredService<ILogger<ScriptEnvironment>>();
        _inputReader = ActionInputReader<string>.Null;
        _outputWriter = ActionOutputWriter<object>.Null;

        // Forwards the following 2 property change notifications as messages on the event bus. They will eventually be pushed to IPC clients.
        Script.OnPropertyChanged.Add(async args =>
        {
            await _eventBus.PublishAsync(new ScriptPropertyChangedEvent(Script.Id, args.PropertyName, args.OldValue,
                args.NewValue));
        });

        Script.Config.OnPropertyChanged.Add(async args =>
        {
            await _eventBus.PublishAsync(new ScriptConfigPropertyChangedEvent(Script.Id, args.PropertyName,
                args.OldValue, args.NewValue));
        });

        _runner = new Lazy<IScriptRunner>(() =>
        {
            var factory = _serviceScope.ServiceProvider.GetRequiredService<IScriptRunnerFactory>();
            var runner = factory.CreateRunner(Script);
            _logger.LogDebug($"Initialized new {nameof(IScriptRunner)}");
            return runner;
        });

        _disposables.Add(_eventBus.Subscribe<ScriptMemCacheItemInfoChangedEvent>(ev =>
        {
            if (ev.ScriptId == Script.Id)
            {
                var oldValue = MemCacheItems;
                var newValue = ev.Items;
                MemCacheItems = newValue;
                _eventBus.PublishAsync(new EnvironmentPropertyChangedEvent(
                    Script.Id,
                    nameof(MemCacheItems),
                    oldValue,
                    newValue));
            }

            return Task.CompletedTask;
        }));
    }

    public Script Script { get; }

    public virtual ScriptStatus Status { get; private set; }

    public double? RunDurationMilliseconds { get; private set; }

    public MemCacheItemInfo[] MemCacheItems { get; private set; } = [];

    public async Task RunAsync(RunOptions runOptions)
    {
        EnsureNotDisposed();

        _logger.LogTrace($"{nameof(RunAsync)} start");

        if (Status.NotIn(ScriptStatus.Ready, ScriptStatus.Error))
        {
            throw new InvalidOperationException(
                $"Script is not in the correct state to run. Status is currently: {Status}");
        }

        await SetStatusAsync(ScriptStatus.Running);

        try
        {
            // Script could have been requested to stop by this point
            if (Status == ScriptStatus.Stopping) return;

            var runResult = await _runner.Value.RunScriptAsync(runOptions);

            await SetRunDurationAsync(
                runResult.IsScriptCompletedSuccessfully
                    ? runResult.DurationMs
                    : null);

            await SetStatusAsync(runResult.IsScriptCompletedSuccessfully || runResult.IsRunCancelled
                ? ScriptStatus.Ready
                : ScriptStatus.Error);

            _logger.LogDebug("Run finished with status: {Status}", Status);
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

    public async Task StopAsync(bool stopRunner)
    {
        EnsureNotDisposed();

        _logger.LogTrace($"{nameof(StopAsync)} start");

        var stopTime = DateTime.Now;
        var wasRunning = Status == ScriptStatus.Running;

        if (!stopRunner && !wasRunning)
        {
            return;
        }

        if (wasRunning)
        {
            await SetStatusAsync(ScriptStatus.Stopping);
        }

        try
        {
            if (_runner.IsValueCreated)
            {
                await _runner.Value.StopScriptAsync();
            }

            if (wasRunning)
            {
                await _outputWriter.WriteAsync(new RawScriptOutput($"Script stopped at: {stopTime}"));
                await SetStatusAsync(ScriptStatus.Ready);
            }
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

        RemoveScriptRunnerIOHandlers();

        _inputReader = inputReader ?? throw new ArgumentNullException(nameof(inputReader));
        _outputWriter = outputWriter;

        AddScriptRunnerIOHandlers();
    }

    public string[] GetUserVisibleAssemblies() => _runner.Value.GetUserVisibleAssemblies();

    public void DumpMemCacheItem(string key)
    {
        if (_runner.IsValueCreated)
        {
            _runner.Value.DumpMemCacheItem(key);
        }
    }

    public void DeleteMemCacheItem(string key)
    {
        if (_runner.IsValueCreated)
        {
            _runner.Value.DeleteMemCacheItem(key);
        }
    }

    public void ClearMemCacheItems()
    {
        if (_runner.IsValueCreated)
        {
            _runner.Value.ClearMemCacheItems();
        }
    }

    private async Task SetStatusAsync(ScriptStatus status)
    {
        if (status == Status)
        {
            return;
        }

        var oldValue = Status;
        Status = status;
        await _eventBus.PublishAsync(new EnvironmentPropertyChangedEvent(Script.Id, nameof(Status), oldValue, status));
    }

    private async Task SetRunDurationAsync(double? runDurationMs)
    {
        var oldValue = RunDurationMilliseconds;
        RunDurationMilliseconds = runDurationMs;
        await _eventBus.PublishAsync(new EnvironmentPropertyChangedEvent(Script.Id, nameof(RunDurationMilliseconds),
            oldValue, runDurationMs));
    }

    private void AddScriptRunnerIOHandlers()
    {
        _runner.Value.AddInput(_inputReader);
        _runner.Value.AddOutput(_outputWriter);
    }

    private void RemoveScriptRunnerIOHandlers()
    {
        _runner.Value.RemoveInput(_inputReader);
        _runner.Value.RemoveOutput(_outputWriter);
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

        await StopAsync(true);

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
            Try.Run(() => AsyncUtil.RunSync(async () => await StopAsync(true)));

            Script.RemoveAllPropertyChangedHandlers();
            Script.Config.RemoveAllPropertyChangedHandlers();
            foreach (var disposable in _disposables) disposable.Dispose();

            _inputReader = ActionInputReader<string>.Null;
            _outputWriter = ActionOutputWriter<object>.Null;

            try
            {
                _runner.Value.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing runner of type: {Type}", _runner.GetType().FullName);
            }

            _runner = null!;

            _serviceScope.Dispose();
            _serviceScope = null!;
        }
    }

    protected async ValueTask DisposeAsyncCore()
    {
        await Try.RunAsync(async () => await StopAsync(true));

        Script.RemoveAllPropertyChangedHandlers();
        Script.Config.RemoveAllPropertyChangedHandlers();
        foreach (var disposable in _disposables) disposable.Dispose();

        try
        {
            _runner.Value.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing runner of type: {Type}", _runner.GetType().FullName);
        }

        _runner = null!;

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
