using System.Collections.Concurrent;
using System.Collections.Generic;
using NetPad.Dtos;
using NetPad.Events;
using NetPad.IO;
using NetPad.Presentation;
using NetPad.Scripts;
using NetPad.Scripts.Events;

namespace NetPad.Services;

/// <summary>
/// Captures script output from GUI runs so it can be returned via API endpoints.
/// Output is buffered per-script and evicted after read.
/// </summary>
public sealed class ScriptOutputCaptureService : IDisposable
{
    private const int MaxOutputSize = 100 * 1024; // ~100KB, matching the headless path

    private static readonly TimeSpan _bufferTtl = TimeSpan.FromMinutes(10);
    private readonly ConcurrentDictionary<Guid, CaptureContext> _captures = new();
    private readonly System.Timers.Timer _evictionTimer;

    public ScriptOutputCaptureService(IEventBus eventBus)
    {
        eventBus.Subscribe<EnvironmentPropertyChangedEvent>(OnEnvironmentPropertyChanged, useStrongReferences: true);

        // Periodically evict abandoned capture buffers to prevent memory leaks
        // (e.g., client calls StartCapture but never retrieves output)
        _evictionTimer = new System.Timers.Timer(_bufferTtl.TotalMilliseconds) { AutoReset = true };
        _evictionTimer.Elapsed += (_, _) => EvictStaleCaptures();
        _evictionTimer.Start();
    }

    /// <summary>
    /// Begin capturing output for a script run. Adds a capture output writer to the environment.
    /// </summary>
    public void StartCapture(Guid scriptId, ScriptEnvironment environment)
    {
        // Clean up any previous capture for this script
        StopCapture(scriptId);

        var context = new CaptureContext(environment);
        _captures[scriptId] = context;
        environment.AddOutput(context.Writer);
    }

    /// <summary>
    /// Gets captured output for a script. If <paramref name="wait"/> is true, blocks until the run completes.
    /// Evicts the capture after returning.
    /// </summary>
    public async Task<HeadlessRunResult> GetCapturedOutputAsync(
        Guid scriptId, bool wait, int? timeoutMs, CancellationToken cancellationToken)
    {
        if (!_captures.TryGetValue(scriptId, out var context))
        {
            return new HeadlessRunResult
            {
                Status = HeadlessRunResult.StatusFailed,
                Success = false,
                Error = $"No capture is active for script '{scriptId}'."
            };
        }

        if (wait)
        {
            using var timeoutCts = timeoutMs.HasValue
                ? new CancellationTokenSource(timeoutMs.Value)
                : new CancellationTokenSource(TimeSpan.FromMinutes(5));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

            try
            {
                await context.CompletionSource.Task.WaitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                return BuildResult(scriptId, context, HeadlessRunResult.StatusTimeout);
            }
            catch (OperationCanceledException)
            {
                return BuildResult(scriptId, context, HeadlessRunResult.StatusCancelled);
            }
        }

        var statusOverride = context.CompletionSource.Task.IsCompletedSuccessfully
            ? null
            : HeadlessRunResult.StatusPending;
        return BuildResult(scriptId, context, statusOverride);
    }

    private HeadlessRunResult BuildResult(Guid scriptId, CaptureContext context, string? statusOverride)
    {
        StopCapture(scriptId);

        List<ScriptOutput> output;
        List<string> errors;
        string finalStatus;
        double durationMs;

        lock (context)
        {
            output = new List<ScriptOutput>(context.Output);
            errors = new List<string>(context.Errors);
            finalStatus = statusOverride ?? context.FinalStatus ?? HeadlessRunResult.StatusCompleted;
            durationMs = context.DurationMs;
        }

        bool success = finalStatus == HeadlessRunResult.StatusCompleted;

        return new HeadlessRunResult
        {
            Status = finalStatus,
            Success = success,
            DurationMs = durationMs,
            Output = output,
            CompilationErrors = errors.Count > 0 ? errors : null,
            Error = errors.Count > 0 ? string.Join(Environment.NewLine, errors) : null
        };
    }

    private void StopCapture(Guid scriptId)
    {
        if (_captures.TryRemove(scriptId, out var context))
        {
            context.Environment.RemoveOutput(context.Writer);
        }
    }

    private Task OnEnvironmentPropertyChanged(EnvironmentPropertyChangedEvent ev)
    {
        if (!_captures.TryGetValue(ev.ScriptId, out var context)) return Task.CompletedTask;

        if (ev.PropertyName == nameof(ScriptEnvironment.RunDurationMilliseconds) && ev.NewValue is double durationMs)
        {
            lock (context)
            {
                context.DurationMs = durationMs;
            }
        }
        else if (ev.PropertyName == nameof(ScriptEnvironment.Status) && ev.NewValue is ScriptStatus newStatus)
        {
            if (newStatus is ScriptStatus.Ready or ScriptStatus.Error)
            {
                lock (context)
                {
                    context.FinalStatus = newStatus == ScriptStatus.Error
                        ? HeadlessRunResult.StatusFailed
                        : HeadlessRunResult.StatusCompleted;
                }

                context.CompletionSource.TrySetResult();
            }
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _evictionTimer.Stop();
        _evictionTimer.Dispose();

        foreach (var (scriptId, _) in _captures)
        {
            StopCapture(scriptId);
        }
    }

    private void EvictStaleCaptures()
    {
        var cutoff = DateTime.UtcNow - _bufferTtl;
        foreach (var (scriptId, context) in _captures)
        {
            if (context.CreatedAt < cutoff)
            {
                StopCapture(scriptId);
            }
        }
    }

    private class CaptureContext
    {
        private int _totalOutputSize;
        private bool _outputTruncated;

        public CaptureContext(ScriptEnvironment environment)
        {
            Environment = environment;
            Writer = new ActionOutputWriter<object>((output, _) =>
            {
                if (_outputTruncated || output is not ScriptOutput so) return;

                lock (this)
                {
                    if (so.Kind == ScriptOutputKind.Error)
                    {
                        Errors.Add(so.Body ?? string.Empty);
                        return;
                    }

                    _totalOutputSize += so.Body?.Length ?? 0;
                    if (_totalOutputSize > MaxOutputSize)
                    {
                        _outputTruncated = true;
                        Output.Add(new ScriptOutput(ScriptOutputKind.Result, "[Output truncated — exceeded 100KB limit]"));
                        return;
                    }

                    Output.Add(so);
                }
            });
        }

        public ScriptEnvironment Environment { get; }
        public IOutputWriter<object> Writer { get; }
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public List<ScriptOutput> Output { get; } = [];
        public List<string> Errors { get; } = [];
        public string? FinalStatus { get; set; }
        public double DurationMs { get; set; }
        public TaskCompletionSource CompletionSource { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
