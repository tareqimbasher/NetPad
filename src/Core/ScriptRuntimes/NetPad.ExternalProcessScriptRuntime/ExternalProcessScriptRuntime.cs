using System.Diagnostics;
using Microsoft.Extensions.Logging;
using NetPad.Compilation;
using NetPad.Configuration;
using NetPad.DotNet;
using NetPad.IO;
using NetPad.Packages;
using NetPad.Scripts;

namespace NetPad.Runtimes;

/// <summary>
/// A script runtime that runs scripts in an external isolated process.
///
/// NOTE: If this class is unsealed, IDisposable and IAsyncDisposable implementations must be revised.
/// </summary>
public sealed partial class ExternalProcessScriptRuntime : IScriptRuntime
{
    private readonly Script _script;
    private readonly IDotNetInfo _dotNetInfo;
    private readonly ILogger<ExternalProcessScriptRuntime> _logger;
    private readonly IOutputWriter<object> _output;
    private readonly HashSet<IInputReader<string>> _externalInputReaders;
    private readonly HashSet<IOutputWriter<object>> _externalOutputWriters;
    private readonly DirectoryInfo _externalProcessRootDirectory;
    private readonly RawOutputHandler _rawOutputHandler;
    private ProcessHandler? _processHandler;

    public ExternalProcessScriptRuntime(
        Script script,
        ICodeParser codeParser,
        ICodeCompiler codeCompiler,
        IPackageProvider packageProvider,
        IDotNetInfo dotNetInfo,
        Settings settings,
        ILogger<ExternalProcessScriptRuntime> logger)
    {
        _script = script;
        _codeParser = codeParser;
        _codeCompiler = codeCompiler;
        _packageProvider = packageProvider;
        _dotNetInfo = dotNetInfo;
        _settings = settings;
        _logger = logger;
        _externalInputReaders = new HashSet<IInputReader<string>>();
        _externalOutputWriters = new HashSet<IOutputWriter<object>>();
        _externalProcessRootDirectory = AppDataProvider.ExternalProcessesDirectoryPath
            .Combine(_script.Id.ToString())
            .GetInfo();

        // Forward output to any configured external output writers
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
                catch
                {
                    // ignored
                }
            }
        });

        _rawOutputHandler = new RawOutputHandler(_output);
    }

    public async Task<RunResult> RunScriptAsync(RunOptions runOptions)
    {
        _logger.LogDebug("Starting to run script");

        try
        {
            var runDependencies = await GetRunDependencies(runOptions);

            if (runDependencies == null)
                return RunResult.RunAttemptFailure();

            var scriptAssemblyFilePath = await SetupExternalProcessRootDirectoryAsync(runDependencies);

            // Reset raw output order
            _rawOutputHandler.Reset();

            _processHandler = new ProcessHandler(
                _dotNetInfo.LocateDotNetExecutableOrThrow(),
                $"{scriptAssemblyFilePath.Path} -html"
            );

            _processHandler.IO.OnOutputReceivedHandlers.Add(OnProcessOutputReceived);

            _processHandler.IO.OnErrorReceivedHandlers.Add(async raw =>
                await OnProcessErrorReceived(raw, runDependencies.ParsingResult.UserProgramStartLineNumber));

            var stopWatch = Stopwatch.StartNew();

            var startResult = _processHandler.StartProcess();

            if (!startResult.Success)
            {
                return RunResult.RunAttemptFailure();
            }

            var exitCode = await startResult.WaitForExitTask;

            stopWatch.Stop();
            var elapsed = stopWatch.ElapsedMilliseconds;

            _logger.LogDebug(
                "Script run completed with exit code: {ExitCode}. Duration: {Duration} ms",
                exitCode,
                elapsed);

            return exitCode switch
            {
                0 => RunResult.Success(elapsed),
                -1 => RunResult.RunCancelled(),
                _ => RunResult.ScriptCompletionFailure(elapsed)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error running script");
            await _output.WriteAsync(new ErrorScriptOutput(ex));
            return RunResult.RunAttemptFailure();
        }
        finally
        {
            _ = StopScriptAsync();
        }
    }

    public Task StopScriptAsync()
    {
        if (_processHandler == null) return Task.CompletedTask;

        try
        {
            _processHandler.Dispose();
            _processHandler = null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing process handler");
        }

        _externalProcessRootDirectory.Refresh();

        if (_externalProcessRootDirectory.Exists)
        {
            try
            {
                _externalProcessRootDirectory.Delete(true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not delete process root directory: {Path}. Error: {ErrorMessage}",
                    _externalProcessRootDirectory.FullName,
                    ex.Message);
            }
        }

        return Task.CompletedTask;
    }

    public string[] GetUserAccessibleAssemblies()
    {
        return new[]
        {
            typeof(IOutputWriter<>).Assembly.Location,
            typeof(ExternalProcessOutput).Assembly.Location,
            typeof(Presentation.PresentationSettings).Assembly.Location,
            typeof(O2Html.HtmlSerializer).Assembly.Location,
        };
    }

    public void Dispose()
    {
        _logger.LogTrace("Dispose start");

        _externalOutputWriters.Clear();

        try
        {
            _processHandler?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing process handler");
        }

        _logger.LogTrace("Dispose end");
    }
}
