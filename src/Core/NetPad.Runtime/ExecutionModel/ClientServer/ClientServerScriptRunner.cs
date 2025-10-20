using System.Text.Json;
using Microsoft.Extensions.Logging;
using NetPad.Application;
using NetPad.Compilation.Scripts;
using NetPad.Compilation.Scripts.Dependencies;
using NetPad.Configuration;
using NetPad.Data.Events;
using NetPad.DotNet;
using NetPad.Events;
using NetPad.ExecutionModel.ClientServer.Messages;
using NetPad.ExecutionModel.ClientServer.ScriptHost;
using NetPad.ExecutionModel.External.Interface;
using NetPad.IO;
using NetPad.IO.IPC.Stdio;
using NetPad.Presentation;
using NetPad.Scripts;
using NetPad.Scripts.Events;
using JsonSerializer = NetPad.Common.JsonSerializer;

namespace NetPad.ExecutionModel.ClientServer;

/// <summary>
/// Runs a script by spawning a long-running host process (the Server) called the "script-host".
/// The Client (this) will then interface with the script-host via standard input/output streams (STD IO).
///
/// The script-host process is spawned the first time the script is run and is stopped and restarted under
/// certain conditions, like when the user stops the script or when the target .NET version is changed.
///
/// <para>
/// High-level operation flow of what happens each time a script runs:
/// 1. Sets up the run environment
///     a. Resolves dependencies required by the script or the script-host
///     b. Parses code and adds additional code to the user's script to make a runnable program
///     c. Compiles script into an assembly
///     d. Deploys script-host process, compiled script assembly and dependency assets to filesystem
/// 2. Starts a script-host process (Server) if one hasn't been started already
/// 3. Client (this) sends message, via STD IO, to script-host process to run script assembly
/// 4. script-host loads script assembly and its dependencies and executes main entry point
///     a. script-host remains alive after script execution
///     b. script-host can be terminated under certain conditions (ex. the user stopping the script)
///     c. script-host sends output back to Client (this) by emitting messages
/// </para>
/// </summary>
public sealed partial class ClientServerScriptRunner : IScriptRunner
{
    private readonly Script _script;
    private readonly IScriptDependencyResolver _scriptDependencyResolver;
    private readonly IScriptCompiler _scriptCompiler;
    private readonly IAppStatusMessagePublisher _appStatusMessagePublisher;
    private readonly IDotNetInfo _dotNetInfo;
    private readonly IEventBus _eventBus;
    private readonly Settings _settings;
    private readonly ILogger<ClientServerScriptRunner> _logger;
    private readonly HashSet<IInputReader<string>> _externalInputReaders;
    private readonly HashSet<IOutputWriter<object>> _externalOutputWriters;
    private readonly IOutputWriter<object> _combinedOutputWriter;
    private readonly RawOutputHandler _rawOutputHandler;
    private readonly WorkingDirectory _workingDirectory;
    private readonly List<IDisposable> _subscriptions = [];

    private readonly ScriptHostProcessManager _scriptHostProcessManager;
    private readonly object _runLock = new();
    private ScriptRun? _currentRun;
    private bool _userRequestedStop;
    private bool _restartScriptHostOnNextRun;

    public ClientServerScriptRunner(
        Script script,
        IScriptDependencyResolver scriptDependencyResolver,
        IScriptCompiler scriptCompiler,
        IAppStatusMessagePublisher appStatusMessagePublisher,
        IDotNetInfo dotNetInfo,
        IEventBus eventBus,
        Settings settings,
        ILogger<ClientServerScriptRunner> logger,
        ILoggerFactory loggerFactory)
    {
        _script = script;
        _scriptDependencyResolver = scriptDependencyResolver;
        _scriptCompiler = scriptCompiler;
        _appStatusMessagePublisher = appStatusMessagePublisher;
        _dotNetInfo = dotNetInfo;
        _eventBus = eventBus;
        _settings = settings;
        _logger = logger;

        _workingDirectory = new WorkingDirectory(script.Id);
        _workingDirectory.DeleteIfExists();

        _externalInputReaders = [];
        _externalOutputWriters = [];

        // Forward output to configured external output writers
        _combinedOutputWriter = new AsyncActionOutputWriter<object>(async (output, title) =>
        {
            if (output == null)
            {
                return;
            }

            foreach (var writer in _externalOutputWriters.ToArray())
            {
                try
                {
                    await writer.WriteAsync(output, title);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error forwarding output to writer: {Type}", writer.GetType().FullName);
                }
            }
        });
        _rawOutputHandler = new RawOutputHandler(_combinedOutputWriter);

        _scriptHostProcessManager = new ScriptHostProcessManager(
            _script,
            _workingDirectory,
            AddScriptHostOnMessageReceivedHandlers,
            _rawOutputHandler.RawOutputReceived,
            _rawOutputHandler.RawErrorReceived,
            _eventBus,
            loggerFactory
        );

        _subscriptions.Add(eventBus.Subscribe<DataConnectionResourcesUpdatingEvent>(ev =>
        {
            // script-host should be restarted if data connection resources (code, assemblies, dependencies) change
            if (_currentRun?.DataConnectionId == ev.DataConnection.Id)
            {
                _restartScriptHostOnNextRun = true;
            }

            return Task.CompletedTask;
        }));
    }

    public Task<RunResult> RunScriptAsync(RunOptions runOptions)
    {
        ScriptRun? previousRun;

        lock (_runLock)
        {
            // If the current run is still in progress return the in progress task
            if (_currentRun is { IsComplete: false })
            {
                return _currentRun.Task;
            }

            _logger.LogDebug("Starting to run script");
            previousRun = _currentRun;
            _currentRun = new ScriptRun(_script);

            // Handle when the run is cancelled (when the script is requested to stop)
            _currentRun.CancellationToken.Register(() =>
            {
                _scriptHostProcessManager.StopScriptHost();
                _currentRun.SetResult(RunResult.RunCancelled());
                _eventBus.PublishAsync(new ScriptMemCacheItemInfoChangedEvent(_script.Id, []));
                _ = _appStatusMessagePublisher.PublishAsync(_script.Id, "Stopped");
            });
        }

        _ = Task.Run(async () =>
        {
            try
            {
                // If there was a previous run, check if we need to stop and restart the script-host process
                if (previousRun != null)
                {
                    bool stopScriptHost =
                        _userRequestedStop
                        || _restartScriptHostOnNextRun
                        || previousRun.HasScriptChangedEnoughToRestartScriptHost(_script);

                    if (stopScriptHost)
                    {
                        _logger.LogDebug("Will restart script-host");
                        _scriptHostProcessManager.StopScriptHost();
                    }
                }

                _userRequestedStop = false;
                _restartScriptHostOnNextRun = false;
                _rawOutputHandler.Reset();

                if (_currentRun.CancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var setup = await SetupRunEnvironmentAsync(runOptions, _currentRun.CancellationToken);

                if (_currentRun.CancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (setup == null)
                {
                    _logger.LogError("Run environment setup failed");
                    _ = _appStatusMessagePublisher.PublishAsync(_script.Id, "Could not run script");
                    _currentRun.SetResult(RunResult.RunAttemptFailure());
                    return;
                }

                _currentRun.UserProgramStartLineNumber = setup.UserProgramStartLineNumber;

                _ = _appStatusMessagePublisher.PublishAsync(_script.Id, "Running...");
                _scriptHostProcessManager.RunScript(
                    _currentRun.RunId,
                    _workingDirectory.SharedDependenciesDirectory,
                    setup.ScriptDir,
                    setup.ScriptAssemblyFilePath,
                    setup.InPlaceDependencyDirectories);

                var result = await _currentRun.Task;
                _logger.LogDebug("Script run completed. Run result: {Result}", result);

                if (!result.IsRunCancelled)
                {
                    _ = _appStatusMessagePublisher.PublishAsync(_script.Id, "Finished");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running script");
                await _combinedOutputWriter.WriteAsync(new ErrorScriptOutput(ex));
                _currentRun.SetResult(RunResult.RunAttemptFailure());
                _ = _appStatusMessagePublisher.PublishAsync(_script.Id, "Script finished with an error");
            }
        });

        return _currentRun.Task;
    }

    public Task StopScriptAsync()
    {
        lock (_runLock)
        {
            _userRequestedStop = true;
            if (_currentRun != null)
            {
                if (!_currentRun.IsComplete)
                {
                    _ = _appStatusMessagePublisher.PublishAsync(_script.Id, "Stopping...");
                }

                _currentRun.Cancel();
                return _currentRun.Task;
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Adds listeners that react to messages output by the script-host process.
    /// </summary>
    private void AddScriptHostOnMessageReceivedHandlers(StdioIpcGateway ipcGateway)
    {
        ipcGateway.On<RequestUserInputMessage>(OnRequestUserInputMessage);
        ipcGateway.On<ScriptOutputMessage>(OnScriptOutputMessage);
        ipcGateway.On<ScriptRunCompleteMessage>(OnScriptRunCompleteMessage);
        ipcGateway.On<ScriptHostExitedMessage>(OnScriptHostExitedMessage);
        ipcGateway.On<MemCacheItemInfoChangedMessage>(msg =>
            _eventBus.PublishAsync(new ScriptMemCacheItemInfoChangedEvent(_script.Id, msg.Items)));
    }

    private async void OnRequestUserInputMessage(RequestUserInputMessage _)
    {
        try
        {
            var run = _currentRun;
            string? input = null;
            foreach (var inputReader in _externalInputReaders.ToArray())
            {
                try
                {
                    if (run?.CancellationToken.IsCancellationRequested == true) return;
                    input = await inputReader.ReadAsync().ConfigureAwait(false);

                    // The first reader that returns a non-null input will be used
                    if (input != null)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Input reader failed: {Type}", inputReader.GetType().FullName);
                }
            }

            if (run?.CancellationToken.IsCancellationRequested == true) return;
            _scriptHostProcessManager.Send(new ReceiveUserInputMessage(input));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error processing RequestUserInputMessage");
        }
    }

    private async void OnScriptOutputMessage(ScriptOutputMessage message)
    {
        try
        {
            var raw = message.Output;

            string type;
            JsonElement outputProperty;

            try
            {
                var json = JsonDocument.Parse(raw).RootElement;
                type =
                    json.GetProperty(nameof(ExternalProcessOutput.Type).ToLowerInvariant()).GetString() ??
                    string.Empty;
                outputProperty = json.GetProperty(nameof(ExternalProcessOutput.Output).ToLowerInvariant());
            }
            catch
            {
                _logger.LogDebug(
                    "Script output is not JSON or could not be deserialized. Output: '{RawOutput}'",
                    raw);
                _rawOutputHandler.RawErrorReceived(raw);
                return;
            }

            ScriptOutput output;

            if (type == nameof(HtmlResultsScriptOutput))
            {
                output = JsonSerializer.Deserialize<HtmlResultsScriptOutput>(outputProperty.ToString())
                         ?? throw new FormatException(
                             $"Could deserialize JSON to {nameof(HtmlResultsScriptOutput)}");
            }
            else if (type == nameof(HtmlSqlScriptOutput))
            {
                output = JsonSerializer.Deserialize<HtmlSqlScriptOutput>(outputProperty.ToString())
                         ?? throw new FormatException(
                             $"Could deserialize JSON to {nameof(HtmlSqlScriptOutput)}");
            }
            else if (type == nameof(HtmlErrorScriptOutput))
            {
                output = JsonSerializer.Deserialize<HtmlErrorScriptOutput>(outputProperty.ToString())
                         ?? throw new FormatException(
                             $"Could deserialize JSON to {nameof(HtmlErrorScriptOutput)}");
            }
            else
            {
                // The raw output handler will handle writing the output
                _rawOutputHandler.RawOutputReceived(raw);
                return;
            }

            await _combinedOutputWriter.WriteAsync(output);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error processing ScriptOutputMessage");
        }
    }

    private void OnScriptRunCompleteMessage(ScriptRunCompleteMessage message)
    {
        try
        {
            if (message.Error != null)
            {
                var error = message.Error;

                if (_currentRun?.UserProgramStartLineNumber != null)
                {
                    error = CorrectUncaughtExceptionStackTraceLineNumber(
                        error,
                        _currentRun.UserProgramStartLineNumber.Value);
                }

                _rawOutputHandler.RawErrorReceived(error);
            }

            _restartScriptHostOnNextRun = message.RestartScriptHostOnNextRun;
            _currentRun?.SetResult(message.Result);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error processing ScriptRunCompleteMessage");
        }
    }

    private async void OnScriptHostExitedMessage(ScriptHostExitedMessage _)
    {
        try
        {
            if (!_userRequestedStop && _currentRun is { IsComplete: false })
            {
                _logger.LogError("script-host process stopped unexpectedly");
                await _combinedOutputWriter.WriteAsync(
                    new ErrorScriptOutput("script-host process stopped unexpectedly"));
                _currentRun.SetResult(RunResult.RunAttemptFailure());
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error processing ScriptHostExitedMessage");
        }
    }

    public void DumpMemCacheItem(string key)
    {
        _scriptHostProcessManager.Send(new DumpMemCacheItemMessage(key));
    }

    public void DeleteMemCacheItem(string key)
    {
        _scriptHostProcessManager.Send(new DeleteMemCacheItemMessage(key));
    }

    public void ClearMemCacheItems()
    {
        _scriptHostProcessManager.Send(new ClearMemCacheMessage());
    }

    /// <summary>
    /// Corrects line numbers in stack trace messages of uncaught exceptions outputted by external running process,
    /// relative to the line number where user code starts.
    /// </summary>
    private static string CorrectUncaughtExceptionStackTraceLineNumber(string output, int userProgramStartLineNumber)
    {
        if (!output.Contains(" :line "))
        {
            return output;
        }

        var lines = output.Split('\n');
        for (var iLine = 0; iLine < lines.Length; iLine++)
        {
            var line = lines[iLine];
            if (line.Contains(" :line "))
            {
                var lineParts = line.Split(" :line ");
                var last = lineParts[^1].Trim();

                if (int.TryParse(last, out int lineNumber))
                {
                    lines[iLine] = line[..(line.Length - last.Length - 1)] +
                                   (lineNumber - userProgramStartLineNumber);
                }
            }
        }

        return string.Join("\n", lines);
    }

    public void Dispose()
    {
        _logger.LogTrace("Dispose start");

        _externalOutputWriters.Clear();
        _externalInputReaders.Clear();

        try
        {
            StopScriptAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping script");
        }

        foreach (var subscription in _subscriptions)
        {
            try
            {
                subscription.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing a subscription: {Subscription}", subscription.ToString());
            }
        }

        _logger.LogTrace("Dispose end");
    }
}
