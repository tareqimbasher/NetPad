using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.Dtos;
using NetPad.Events;
using NetPad.ExecutionModel;
using NetPad.ExecutionModel.External;
using NetPad.IO;
using NetPad.Presentation;
using NetPad.Scripts;
using NetPad.Scripts.Events;
using NetPad.Sessions;

namespace NetPad.Services;

public class HeadlessScriptExecutionService(
    HeadlessScriptRunnerFactory runnerFactory,
    IDataConnectionRepository dataConnectionRepository,
    IDotNetInfo dotNetInfo,
    ISession session,
    IScriptRepository scriptRepository,
    ScriptOutputCaptureService captureService,
    IEventBus eventBus,
    ILogger<HeadlessScriptExecutionService> logger)
{
    private const int MaxOutputSize = 100 * 1024; // ~100KB

    public async Task<HeadlessRunResult> RunCodeAsync(HeadlessRunRequest request, CancellationToken cancellationToken)
    {
        var targetFramework = request.TargetFramework
                              ?? dotNetInfo.GetLatestSupportedDotNetSdkVersion()?.GetFrameworkVersion();

        if (targetFramework == null)
        {
            return new HeadlessRunResult
            {
                Status = HeadlessRunResult.StatusFailed,
                Success = false,
                Error = "Could not determine a .NET SDK version. Ensure a supported .NET SDK is installed."
            };
        }

        var kind = request.Kind?.Equals("sql", StringComparison.OrdinalIgnoreCase) == true
            ? ScriptKind.SQL
            : ScriptKind.Program;

        var script = new Script(
            ScriptIdGenerator.NewId(),
            "Headless Script",
            new ScriptConfig(
                kind,
                targetFramework.Value,
                namespaces: ScriptConfigDefaults.DefaultNamespaces,
                optimizationLevel: OptimizationLevel.Debug
            ),
            request.Code
        );

        if (request.DataConnectionId != null)
        {
            var dataConnection = await dataConnectionRepository.GetAsync(request.DataConnectionId.Value);
            if (dataConnection != null)
            {
                script.SetDataConnection(dataConnection);
            }
        }

        // TODO: Add NuGet package references when PackageReference support is available for headless scripts

        return await ExecuteScriptAsync(script, request.TimeoutMs, cancellationToken);
    }

    public async Task<HeadlessRunResult> RunScriptAsync(Guid scriptId, int? timeoutMs, CancellationToken cancellationToken)
    {
        // Try to find the script in the current session first, then fall back to repository
        var script = session.Get(scriptId)?.Script
                     ?? await scriptRepository.GetAsync(scriptId);

        if (script == null)
        {
            return new HeadlessRunResult
            {
                Status = HeadlessRunResult.StatusFailed,
                Success = false,
                Error = $"Script with ID '{scriptId}' not found."
            };
        }

        return await ExecuteScriptAsync(script, timeoutMs, cancellationToken);
    }

    /// <summary>
    /// Runs a script that is currently open in the GUI, capturing its output.
    /// The script executes through the normal GUI flow (status updates visible in the UI).
    /// </summary>
    public async Task<HeadlessRunResult> RunScriptInGuiAsync(Guid scriptId, int? timeoutMs, CancellationToken cancellationToken)
    {
        var environment = session.Get(scriptId);

        if (environment == null)
        {
            return new HeadlessRunResult
            {
                Status = HeadlessRunResult.StatusFailed,
                Success = false,
                Error = $"Script with ID '{scriptId}' is not open in the GUI."
            };
        }

        captureService.StartCapture(scriptId, environment);

        // Run through the environment (same as the GUI's RunScriptCommand)
        await environment.RunAsync(new RunOptions());
        await eventBus.PublishAsync(new ScriptRanEvent(environment));

        return await captureService.GetCapturedOutputAsync(scriptId, wait: true, timeoutMs, cancellationToken);
    }

    private async Task<HeadlessRunResult> ExecuteScriptAsync(Script script, int? timeoutMs, CancellationToken cancellationToken)
    {
        var output = new List<ScriptOutput>();
        var errors = new List<string>();
        int totalOutputSize = 0;
        bool outputTruncated = false;

        using var runner = runnerFactory.CreateRunner(script);

        runner.AddOutput(new ActionOutputWriter<object>((o, _) =>
        {
            if (outputTruncated || o is not ScriptOutput so) return;

            if (so.Kind == ScriptOutputKind.Error)
            {
                errors.Add(so.Body ?? string.Empty);
                return;
            }

            totalOutputSize += so.Body?.Length ?? 0;
            if (totalOutputSize > MaxOutputSize)
            {
                outputTruncated = true;
                output.Add(new ScriptOutput(ScriptOutputKind.Result, "[Output truncated — exceeded 100KB limit]"));
                return;
            }

            output.Add(so);
        }));

        var runOptions = new RunOptions();
        runOptions.SetOption(new ExternalScriptRunnerOptions
        {
            NoCache = true,
            RedirectIo = true,
            ProcessCliArgs = ["-json-msg"]
        });

        RunResult runResult;
        try
        {
            var runTask = runner.RunScriptAsync(runOptions);

            // RunScriptAsync does not accept a CancellationToken — the external runner spawns a
            // process and waits for it to exit. To enforce timeout/cancellation we race the run
            // task against a delay and call StopScriptAsync if the deadline is reached.
            if (timeoutMs.HasValue || cancellationToken.CanBeCanceled)
            {
                using var timeoutCts = timeoutMs.HasValue
                    ? new CancellationTokenSource(timeoutMs.Value)
                    : new CancellationTokenSource();
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

                // Use a TCS + token registration instead of Task.Delay to avoid unobserved
                // TaskCanceledException and dangling tasks.
                var cancelTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                using var registration = linkedCts.Token.Register(() => cancelTcs.TrySetResult());

                var completed = await Task.WhenAny(runTask, cancelTcs.Task);

                if (completed != runTask)
                {
                    // Deadline or caller cancellation reached — stop the script
                    await runner.StopScriptAsync();
                    runResult = await runTask; // Let it finish after stop

                    bool isTimeout = timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested;
                    return new HeadlessRunResult
                    {
                        Status = isTimeout ? HeadlessRunResult.StatusTimeout : HeadlessRunResult.StatusCancelled,
                        Success = false,
                        DurationMs = runResult.DurationMs,
                        Output = output,
                        Error = isTimeout ? $"Execution timed out after {timeoutMs}ms." : null
                    };
                }
            }

            runResult = await runTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing headless script");
            return new HeadlessRunResult
            {
                Status = HeadlessRunResult.StatusFailed,
                Success = false,
                Output = output,
                Error = ex.Message
            };
        }

        string status;
        if (runResult.IsScriptCompletedSuccessfully)
            status = HeadlessRunResult.StatusCompleted;
        else if (runResult.IsRunCancelled)
            status = HeadlessRunResult.StatusCancelled;
        else
            status = HeadlessRunResult.StatusFailed;

        return new HeadlessRunResult
        {
            Status = status,
            Success = runResult.IsScriptCompletedSuccessfully,
            DurationMs = runResult.DurationMs,
            Output = output,
            CompilationErrors = errors.Count > 0 ? errors : null,
            Error = !runResult.IsScriptCompletedSuccessfully && errors.Count > 0
                ? string.Join(Environment.NewLine, errors)
                : null
        };
    }
}
