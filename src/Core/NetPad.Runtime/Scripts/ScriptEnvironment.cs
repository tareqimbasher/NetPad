using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Common;
using NetPad.Events;
using NetPad.ExecutionModel;
using NetPad.ExecutionModel.ClientServer.Events;
using NetPad.ExecutionModel.ScriptServices;
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
/// Call <see cref="AddOutput"/> and <see cref="SetInput"/> to hook up
/// <see cref="IOutputWriter{T}"/> and <see cref="IInputReader{T}"/> implementations for script output and user input.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
/// using var scope = serviceProvider.CreateScope();
/// var environment = new ScriptEnvironment(myScript, scope);
/// environment.SetInput(consoleReader);
/// environment.AddOutput(consoleWriter);
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
    private IInputReader<string>? _inputReader;
    private readonly List<IOutputWriter<object>> _outputWriters = [];
    private readonly AsyncActionOutputWriter<object> _outputForwarder;
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

        _outputForwarder = new AsyncActionOutputWriter<object>(async (output, title) =>
        {
            foreach (var writer in _outputWriters.ToArray())
            {
                try
                {
                    await writer.WriteAsync(output, title);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error forwarding output to writer: {Type}", writer.GetType().FullName);
                }
            }
        });

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
        _runner.AddOutput(_outputForwarder);

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
            await _outputForwarder.WriteAsync(new ScriptOutput(ScriptOutputKind.Error, ex.ToString()));
            SetStatus(ScriptStatus.Error);
        }
        finally
        {
            _logger.LogTrace($"{nameof(RunAsync)} end");
        }
    }

    /// <summary>
    /// Stops the script, and its runner if the script is currently running.
    /// If <paramref name="stopRunner"/> is true and the script is not currently
    /// running, this will also stop the idle runner.
    /// </summary>
    /// <param name="stopRunner">
    /// If true, will stop the runner even if the script is not currently running.
    /// The runner is always stopped regardless of the value of this param if the
    /// script is currently running.
    /// </param>
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
                await _outputForwarder.WriteAsync(new ScriptOutput(ScriptOutputKind.Result, $"Script stopped at: {stopTime}"));
                SetStatus(ScriptStatus.Ready);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping script");
            await _outputForwarder.WriteAsync(new ScriptOutput(ScriptOutputKind.Error, ex.ToString()));
            SetStatus(ScriptStatus.Error);
        }
        finally
        {
            _logger.LogTrace($"{nameof(StopAsync)} end");
        }
    }

    /// <summary>
    /// Sets the input reader that supplies user input to the script.
    /// Only one input reader can be active at a time; setting a new one replaces the previous.
    /// </summary>
    public void SetInput(IInputReader<string> inputReader)
    {
        EnsureNotDisposed();

        if (_inputReader != null)
        {
            _runner.RemoveInput(_inputReader);
        }

        _inputReader = inputReader ?? throw new ArgumentNullException(nameof(inputReader));
        _runner.AddInput(_inputReader);
    }

    /// <summary>
    /// Removes the current input reader, if any.
    /// </summary>
    public void RemoveInput()
    {
        if (_inputReader != null)
        {
            _runner.RemoveInput(_inputReader);
            _inputReader = null;
        }
    }

    /// <summary>
    /// Adds an output writer that receives script output.
    /// Multiple output writers can be active simultaneously.
    /// </summary>
    public void AddOutput(IOutputWriter<object> outputWriter)
    {
        EnsureNotDisposed();
        _outputWriters.Add(outputWriter ?? throw new ArgumentNullException(nameof(outputWriter)));
    }

    /// <summary>
    /// Removes a previously added output writer.
    /// </summary>
    public void RemoveOutput(IOutputWriter<object> outputWriter)
    {
        _outputWriters.Remove(outputWriter);
    }

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

            _outputWriters.Clear();
            RemoveInput();

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

        _outputWriters.Clear();
        RemoveInput();

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
