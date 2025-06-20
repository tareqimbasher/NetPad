using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Common;
using NetPad.Events;
using NetPad.ExecutionModel;
using NetPad.ExecutionModel.ClientServer.Events;
using NetPad.ExecutionModel.ClientServer.ScriptServices;
using NetPad.IO;
using NetPad.Presentation;
using NetPad.Scripts.Events;

namespace NetPad.Scripts;

/// <summary>
/// Provides a managed execution context for a <see cref="Script"/> used to run it, track its status, receive
/// script output and provide input when requested. Many high level operations throughout the application run against
/// a <see cref="ScriptEnvironment"/> instead of the <see cref="Script"/> itself.
/// </summary>
/// <remarks>
/// <para>
/// All changes to the properties of this environment or changes to the properties of the script or its configuration
/// are published via the injected <see cref="IEventBus"/>, including:
/// <list type="bullet">
///   <item><see cref="ScriptPropertyChangedEvent"/> and <see cref="ScriptConfigPropertyChangedEvent"/> for changes in the <see cref="Script"/> or its configuration.</item>
///   <item><c>EnvironmentPropertyChangedEvent</c> for changes in the <see cref="ScriptEnvironment"/></item>
/// </list>
/// This lets UI or IPC clients subscribe and react to progress, errors, or memory-cache updates in real time.
/// </para>
/// <para>
/// Call <see cref="SetIO(IInputReader{string}, IOutputWriter{object})"/> to hook up
/// <see cref="IInputReader{T}"/> and <see cref="IOutputWriter{T}"/> implementations for user input and script output.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using var scope = serviceProvider.CreateScope();
/// var environment = new ScriptEnvironment(myScript, scope);
/// environment.SetIO(consoleReader, consoleWriter);
/// await environment.RunAsync(new RunOptions());
/// </code>
/// </example>

// Needed for testing with Moq:
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class ScriptEnvironment : IDisposable, IAsyncDisposable
{
    private readonly IEventBus _eventBus;
    private readonly ILogger<ScriptEnvironment> _logger;
    private IServiceScope _serviceScope;
    private IInputReader<string> _inputReader;
    private IOutputWriter<object> _outputWriter;
    private IScriptRunner _runner;
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

        var factory = _serviceScope.ServiceProvider.GetRequiredService<IScriptRunnerFactory>();
        _runner = factory.CreateRunner(Script);

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

        _disposables.Add(_eventBus.Subscribe<ScriptHostProcessLifetimeEvent>(ev =>
        {
            if (ev.ScriptId == Script.Id)
            {
                var oldValue = IsScriptHostRunning;
                var newValue = ev.IsRunning;
                if (oldValue != newValue)
                {
                    IsScriptHostRunning = newValue;
                    _eventBus.PublishAsync(new EnvironmentPropertyChangedEvent(
                        Script.Id,
                        nameof(IsScriptHostRunning),
                        oldValue,
                        newValue));
                }
            }

            return Task.CompletedTask;
        }));
    }

    public Script Script { get; }

    public virtual ScriptStatus Status { get; private set; }

    public double? RunDurationMilliseconds { get; private set; }

    public MemCacheItemInfo[] MemCacheItems { get; private set; } = [];

    public bool IsScriptHostRunning { get; private set; }

    /// <summary>
    /// Runs the script and returns a task that completes when the run completes.
    /// </summary>
    public async Task RunAsync(RunOptions runOptions)
    {
        EnsureNotDisposed();

        _logger.LogTrace($"{nameof(RunAsync)} start");

        if (Status.NotIn(ScriptStatus.Ready, ScriptStatus.Error))
        {
            throw new InvalidOperationException(
                $"Script is not in the correct state to run. Status is currently: {Status}");
        }

        SetStatus(ScriptStatus.Running);

        try
        {
            // Script could have been requested to stop by this point
            if (Status == ScriptStatus.Stopping)
            {
                return;
            }

            // If no specific code to run was specified in runOptions, set the property to the entire script's code
            // so we capture the current "state" of the code. We don't want changes the user makes to the code after
            // they've hit "Run", and before their code compiles to make it into the run/execution.
            //
            // Users expect that once they hit "Run", any changes they make to the code afterward should not change
            // the code being executed in the "current run".
            runOptions.SpecificCodeToRun ??= Script.Code;

            var runResult = await _runner.RunScriptAsync(runOptions);

            SetRunDuration(runResult.IsScriptCompletedSuccessfully
                ? runResult.DurationMs
                : null);

            SetStatus(runResult.IsScriptCompletedSuccessfully || runResult.IsRunCancelled
                ? ScriptStatus.Ready
                : ScriptStatus.Error);

            _logger.LogDebug("Run finished with status: {Status}", Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running script");
            await _outputWriter.WriteAsync(new ErrorScriptOutput(ex));
            SetStatus(ScriptStatus.Error);
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
            SetStatus(ScriptStatus.Stopping);
        }

        try
        {
            await _runner.StopScriptAsync();

            if (wasRunning)
            {
                await _outputWriter.WriteAsync(new RawScriptOutput($"Script stopped at: {stopTime}"));
                SetStatus(ScriptStatus.Ready);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping script");
            await _outputWriter.WriteAsync(new ErrorScriptOutput(ex));
            SetStatus(ScriptStatus.Error);
        }
        finally
        {
            _logger.LogTrace($"{nameof(StopAsync)} end");
        }
    }

    /// <summary>
    /// Configures the script environment’s input and output handlers by attaching the specified reader and writer
    /// to the underlying <see cref="IScriptRunner"/>.
    /// </summary>
    /// <param name="inputReader">
    /// The <see cref="IInputReader{T}"/> implementation that supplies user input to the script.
    /// Cannot be <c>null</c>.
    /// </param>
    /// <param name="outputWriter">
    /// The <see cref="IOutputWriter{T}"/> implementation that receives script output.
    /// Cannot be <c>null</c>.
    /// </param>
    /// <remarks>
    /// Any previously registered IO handlers are removed before the new ones are added.
    /// You should call this before invoking <see cref="RunAsync(RunOptions)"/> to ensure the script
    /// uses the correct input and output streams.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="inputReader"/> or <paramref name="outputWriter"/> are <c>null</c>.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the <see cref="ScriptEnvironment"/> has already been disposed.
    /// </exception>
    public void SetIO(IInputReader<string> inputReader, IOutputWriter<object> outputWriter)
    {
        EnsureNotDisposed();

        RemoveScriptRunnerIOHandlers();

        _inputReader = inputReader ?? throw new ArgumentNullException(nameof(inputReader));
        _outputWriter = outputWriter ?? throw new ArgumentNullException(nameof(outputWriter));

        AddScriptRunnerIOHandlers();
    }

    public string[] GetUserVisibleAssemblies() => _runner.GetUserVisibleAssemblies();

    public void DumpMemCacheItem(string key)
    {
        _runner.DumpMemCacheItem(key);
    }

    public void DeleteMemCacheItem(string key)
    {
        _runner.DeleteMemCacheItem(key);
    }

    public void ClearMemCacheItems()
    {
        _runner.ClearMemCacheItems();
    }

    private void SetStatus(ScriptStatus status)
    {
        if (status == Status)
        {
            return;
        }

        var oldValue = Status;
        Status = status;
        _ = _eventBus.PublishAsync(new EnvironmentPropertyChangedEvent(Script.Id, nameof(Status), oldValue, status));
    }

    private void SetRunDuration(double? runDurationMs)
    {
        var oldValue = RunDurationMilliseconds;
        RunDurationMilliseconds = runDurationMs;
        _ = _eventBus.PublishAsync(new EnvironmentPropertyChangedEvent(
            Script.Id,
            nameof(RunDurationMilliseconds),
            oldValue,
            runDurationMs)
        );
    }

    private void AddScriptRunnerIOHandlers()
    {
        _runner.AddInput(_inputReader);
        _runner.AddOutput(_outputWriter);
    }

    private void RemoveScriptRunnerIOHandlers()
    {
        _runner.RemoveInput(_inputReader);
        _runner.RemoveOutput(_outputWriter);
    }

    private void EnsureNotDisposed()
    {
        if (_isDisposed)
        {
            throw new InvalidOperationException($"Script environment {Script.Id} is disposed.");
        }
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
                _runner.Dispose();
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

    private async ValueTask DisposeAsyncCore()
    {
        await Try.RunAsync(async () => await StopAsync(true));

        Script.RemoveAllPropertyChangedHandlers();
        Script.Config.RemoveAllPropertyChangedHandlers();
        foreach (var disposable in _disposables) disposable.Dispose();

        try
        {
            _runner.Dispose();
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
