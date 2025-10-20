using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;
using NetPad.Compilation;
using NetPad.Configuration;
using NetPad.Data.Metadata;
using NetPad.DotNet;
using NetPad.IO;
using NetPad.Packages;
using NetPad.Presentation;
using NetPad.Scripts;

namespace NetPad.ExecutionModel.External;

/// <summary>
/// A script runtime that runs scripts in an external isolated process.
///
/// NOTE: If this class is unsealed, IDisposable and IAsyncDisposable implementations must be revised.
/// </summary>
public sealed partial class ExternalScriptRunner : IScriptRunner
{
    private readonly Script _script;
    private readonly IDataConnectionResourcesCache _dataConnectionResourcesCache;
    private readonly IDotNetInfo _dotNetInfo;
    private readonly ILogger<ExternalScriptRunner> _logger;
    private readonly IOutputWriter<object> _output;
    private readonly HashSet<IInputReader<string>> _externalInputReaders;
    private readonly HashSet<IOutputWriter<object>> _externalOutputWriters;
    private readonly DeploymentCache _deploymentCache;
    private readonly RawOutputHandler _rawOutputHandler;
    private ProcessStartResult? _scriptProcess;

    private static readonly string[] _supportAssemblies =
    [
        typeof(Dumpify.DumpExtensions).Assembly.Location,
        typeof(Spectre.Console.IAnsiConsole).Assembly.Location
    ];

    public ExternalScriptRunner(
        Script script,
        ICodeParser codeParser,
        ICodeCompiler codeCompiler,
        IPackageProvider packageProvider,
        IDataConnectionResourcesCache dataConnectionResourcesCache,
        IDotNetInfo dotNetInfo,
        Settings settings,
        ILogger<ExternalScriptRunner> logger)
    {
        _script = script;
        _dataConnectionResourcesCache = dataConnectionResourcesCache;
        _codeParser = codeParser;
        _codeCompiler = codeCompiler;
        _packageProvider = packageProvider;
        _dotNetInfo = dotNetInfo;
        _settings = settings;
        _logger = logger;

        _externalInputReaders = [];
        _externalOutputWriters = [];
        _deploymentCache = new DeploymentCache(AppDataProvider.ExternalExecutionModelDeploymentCacheDirectoryPath);

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
    }

    public async Task<RunResult> RunScriptAsync(RunOptions runOptions)
    {
        _logger.LogDebug("Starting to run script");

        DeploymentDirectory? deploymentDir = null;

        try
        {
            if (!runOptions.TryGet<ExternalScriptRunnerOptions>(out var options))
            {
                throw new InvalidOperationException($"{nameof(ExternalScriptRunnerOptions)} are required.");
            }

            deploymentDir = await BuildAndDeployAsync(runOptions, options.NoCache, options.ForceRebuild);
            var deploymentInfo = deploymentDir?.GetDeploymentInfo();
            if (deploymentDir == null || deploymentInfo == null)
            {
                return RunResult.RunAttemptFailure();
            }

            var scriptAssemblyFilePath = Path.Combine(deploymentDir.Path, deploymentInfo.ScriptAssemblyFileName);

            // Reset raw output order
            _rawOutputHandler.Reset();

            var args = options.ProcessCliArgs.Contains("-parent")
                ? options.ProcessCliArgs
                : options.ProcessCliArgs.Concat(["-parent", Environment.ProcessId.ToString()]).ToArray();

            var startInfo = new ProcessStartInfo(
                    _dotNetInfo.LocateDotNetExecutableOrThrow(),
                    $"\"{scriptAssemblyFilePath}\" -- {string.Join(' ', args)}")
                .CopyCurrentEnvironmentVariables();

            if (options.RedirectIo)
            {
                startInfo
                    .WithRedirectIO()
                    .WithNoUi();
            }

            // On Windows, we need this environment var to force console output when using the ConsoleLoggingProvider
            // See: https://github.com/dotnet/runtime/blob/8a2e7e3e979d671d97cb408fbcbdbee5594479a4/src/libraries/Microsoft.Extensions.Logging.Console/src/ConsoleLoggerProvider.cs#L69
            if (_script.Config.UseAspNet && PlatformUtil.IsOSWindows())
            {
                startInfo.EnvironmentVariables.Add("DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION", "true");
            }

            _scriptProcess = startInfo.Run(
                output => _ = OnProcessOutputReceived(output),
                error => OnProcessErrorReceived(error, deploymentInfo.UserProgramStartLineNumber)
            );

            var runStart = DateTime.Now;
            var stopWatch = Stopwatch.StartNew();

            if (!_scriptProcess.Started)
            {
                return RunResult.RunAttemptFailure();
            }

            var exitCode = await _scriptProcess.WaitForExitTask;

            stopWatch.Stop();
            var elapsed = stopWatch.ElapsedMilliseconds;

            _logger.LogDebug(
                "Script run completed with exit code: {ExitCode}. Duration: {Duration} ms",
                exitCode,
                elapsed);

            // Set last run datetime
            deploymentInfo.LastRunAt = runStart;
            deploymentInfo.LastRunSucceeded = exitCode == 0;
            deploymentDir.SaveDeploymentInfo(deploymentInfo);

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
            if (deploymentDir?.IsTemporary == true)
            {
                Try.Run(() => Directory.Delete(deploymentDir.Path, true));
            }
        }
    }

    public Task StopScriptAsync()
    {
        if (_scriptProcess == null)
        {
            return Task.CompletedTask;
        }

        try
        {
            _scriptProcess.Process.KillIfRunning();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error killing script process");
        }

        _scriptProcess = null;
        return Task.CompletedTask;
    }

    public void DumpMemCacheItem(string key)
    {
    }

    public void DeleteMemCacheItem(string key)
    {
    }

    public void ClearMemCacheItems()
    {
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

        _logger.LogTrace("Dispose end");
    }
}
