using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NetPad.Compilation;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.Data.Events;
using NetPad.DotNet;
using NetPad.Events;
using NetPad.ExecutionModel.ClientServer.Messages;
using NetPad.ExecutionModel.ClientServer.ScriptHost;
using NetPad.ExecutionModel.External;
using NetPad.ExecutionModel.External.Interface;
using NetPad.IO;
using NetPad.Packages;
using NetPad.Presentation;
using NetPad.Scripts;
using O2Html;
using JsonSerializer = NetPad.Common.JsonSerializer;

namespace NetPad.ExecutionModel.ClientServer;

public sealed partial class ClientServerScriptRunner : IScriptRunner
{
    private readonly Script _script;
    private readonly IDataConnectionResourcesCache _dataConnectionResourcesCache;
    private readonly IDotNetInfo _dotNetInfo;
    private readonly ILogger<ClientServerScriptRunner> _logger;
    private readonly DirectoryPath _scriptHostRootDirectory;
    private readonly HashSet<IInputReader<string>> _externalInputReaders;
    private readonly HashSet<IOutputWriter<object>> _externalOutputWriters;
    private readonly IOutputWriter<object> _output;
    private readonly RawOutputHandler _rawOutputHandler;
    private readonly ScriptHostProcessManager _scriptHostProcessManager;
    private readonly List<IDisposable> _subscriptions = new();

    private ScriptRun? _currentScriptRun;
    private bool _userRequestedStop;
    private bool _restartScriptHostOnNextRun;

    private static readonly string[] _userVisibleAssemblies =
    [
        typeof(INetPadRuntimeMarker).Assembly.Location,
        typeof(HtmlSerializer).Assembly.Location
    ];

    public ClientServerScriptRunner(
        Script script,
        ICodeParser codeParser,
        ICodeCompiler codeCompiler,
        IPackageProvider packageProvider,
        IDataConnectionResourcesCache dataConnectionResourcesCache,
        IDotNetInfo dotNetInfo,
        IEventBus eventBus,
        Settings settings,
        ILogger<ClientServerScriptRunner> logger,
        ILoggerFactory loggerFactory)
    {
        _script = script;
        _codeParser = codeParser;
        _codeCompiler = codeCompiler;
        _packageProvider = packageProvider;
        _dataConnectionResourcesCache = dataConnectionResourcesCache;
        _dotNetInfo = dotNetInfo;
        _settings = settings;
        _logger = logger;

        _scriptHostRootDirectory = AppDataProvider.ExternalProcessesDirectoryPath.Combine(_script.Id.ToString());

        var dirInfo = _scriptHostRootDirectory.GetInfo();
        if (dirInfo.Exists)
        {
            dirInfo.Delete(true);
        }

        _externalInputReaders = [];
        _externalOutputWriters = [];

        // Forward output to configured external output writers
        _output = new AsyncActionOutputWriter<object>(async (output, title) =>
        {
            if (output == null)
            {
                return;
            }

            foreach (var writer in _externalOutputWriters)
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
        _rawOutputHandler = new RawOutputHandler(_output);

        _scriptHostProcessManager = CreateScriptHostProcessManager(_scriptHostRootDirectory, loggerFactory);

        _subscriptions.Add(eventBus.Subscribe<DataConnectionResourcesUpdatingEvent>(ev =>
        {
            if (_currentScriptRun?.DataConnectionId == ev.DataConnection.Id)
            {
                _restartScriptHostOnNextRun = true;
            }

            return Task.CompletedTask;
        }));
    }

    public string[] GetUserVisibleAssemblies() => _userVisibleAssemblies;

    private ScriptHostProcessManager CreateScriptHostProcessManager(
        DirectoryPath scriptHostRootDirectory,
        ILoggerFactory loggerFactory)
    {
        // ScriptHost executable should be in the same directory as the NetPad backend, and this assembly.
        FilePath scriptHostExecutablePath = Path.Combine(
            Path.GetDirectoryName(typeof(ClientServerScriptRunner).Assembly.Location) ?? string.Empty,
            "netpad-script-host" + (PlatformUtil.IsOSWindows() ? ".exe" : string.Empty)
        );

        var manager = new ScriptHostProcessManager(
            _script,
            scriptHostExecutablePath,
            scriptHostRootDirectory,
            _rawOutputHandler.RawOutputReceived,
            _rawOutputHandler.RawErrorReceived,
            loggerFactory.CreateLogger(
                $"{typeof(ScriptHostProcessManager)} | [{_script.Id.ToString()[..8]}] {_script.Name}"),
            ipcGateway =>
            {
                ipcGateway.On<HtmlResultsScriptOutput>(msg => _output.WriteAsync(msg));
                ipcGateway.On<HtmlSqlScriptOutput>(msg => _output.WriteAsync(msg));
                ipcGateway.On<HtmlErrorScriptOutput>(msg => _output.WriteAsync(msg));
                ipcGateway.On<RequestUserInputMessage>(_ =>
                {
                    // The first reader that returns a non-null input will be used
                    string? input = null;
                    foreach (var inputReader in _externalInputReaders)
                    {
                        input = inputReader.ReadAsync().Result;
                        if (input != null)
                        {
                            break;
                        }
                    }

                    _scriptHostProcessManager.Send(new ReceiveUserInputMessage(input));
                });

                ipcGateway.On<ScriptOutputMessage>(msg =>
                {
                    try
                    {
                        var raw = msg.Output;

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

                        _output.WriteAsync(output);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error processing script output");
                    }
                });

                ipcGateway.On<ScriptRunCompleteMessage>(msg =>
                {
                    if (msg.Error != null)
                    {
                        var error = msg.Error;

                        if (_currentScriptRun != null)
                        {
                            error = CorrectUncaughtExceptionStackTraceLineNumber(
                                error,
                                _currentScriptRun.UserProgramStartLineNumber);
                        }

                        _rawOutputHandler.RawErrorReceived(error);
                    }

                    _restartScriptHostOnNextRun = msg.RestartScriptHostOnNextRun;
                    _currentScriptRun?.SetResult(msg.Result);
                });

                ipcGateway.On<ScriptHostExitedMessage>(_ =>
                {
                    if (_currentScriptRun is not { IsComplete: false }) return;
                    if (_userRequestedStop)
                    {
                        _logger.LogError("script-host process stopped because user requested stop");
                        _currentScriptRun.SetResult(RunResult.RunCancelled());
                    }
                    else
                    {
                        _logger.LogError("script-host process stopped unexpectedly");
                        _output.WriteAsync(new ErrorScriptOutput("script-host process stopped unexpectedly")).Wait();
                        _currentScriptRun.SetResult(RunResult.RunAttemptFailure());
                    }
                });
            }
        );


        return manager;
    }

    public async Task<RunResult> RunScriptAsync(RunOptions runOptions)
    {
        _logger.LogDebug("Starting to run script");

        try
        {
            if (_currentScriptRun != null && _scriptHostProcessManager.IsScriptHostRunning())
            {
                bool stopScriptHost =
                    _restartScriptHostOnNextRun
                    || _currentScriptRun.HasScriptChangedEnoughToRestartScriptHost(_script);

                if (stopScriptHost)
                {
                    _logger.LogDebug("Will restart script-host");
                    _scriptHostProcessManager.StopScriptHost();
                }
            }

            _restartScriptHostOnNextRun = false;
            _userRequestedStop = false;
            _rawOutputHandler.Reset();

            var setup = await SetupRunEnvironment(runOptions);
            if (setup == null)
            {
                return RunResult.RunAttemptFailure();
            }

            _currentScriptRun = new ScriptRun(Guid.NewGuid(), setup.UserProgramStartLineNumber, _script);

            _scriptHostProcessManager.RunScript(
                _currentScriptRun.RunId,
                setup.ScriptHostDepsDir,
                setup.ScriptDir,
                setup.ScriptAssemblyFilePath,
                setup.InPlaceDependencyDirectories);

            var result = await _currentScriptRun.Task;
            _logger.LogDebug("Script run completed. Run result: {Result}", result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running script");
            await _output.WriteAsync(new ErrorScriptOutput(ex));
            return RunResult.RunAttemptFailure();
        }
    }

    public Task StopScriptAsync()
    {
        _userRequestedStop = true;
        _scriptHostProcessManager.StopScriptHost();
        return Task.CompletedTask;
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

        try
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing subscriptions");
        }

        _logger.LogTrace("Dispose end");
    }
}
