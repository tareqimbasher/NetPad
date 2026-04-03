using System.Collections.Concurrent;
using System.Collections.Generic;
using NetPad.Dtos;
using NetPad.Events;
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
    private static readonly TimeSpan _bufferTtl = TimeSpan.FromMinutes(10);
    private readonly ConcurrentDictionary<Guid, CaptureBuffer> _buffers = new();
    private readonly System.Timers.Timer _evictionTimer;

    public ScriptOutputCaptureService(IEventBus eventBus)
    {
        eventBus.Subscribe<EnvironmentPropertyChangedEvent>(OnEnvironmentPropertyChanged, useStrongReferences: true);

        // Periodically evict abandoned capture buffers to prevent memory leaks
        // (e.g., client calls StartCapture but never retrieves output)
        _evictionTimer = new System.Timers.Timer(_bufferTtl.TotalMilliseconds) { AutoReset = true };
        _evictionTimer.Elapsed += (_, _) => EvictStaleBuffers();
        _evictionTimer.Start();
    }

    /// <summary>
    /// Begin capturing output for a script run.
    /// </summary>
    public void StartCapture(Guid scriptId)
    {
        _buffers[scriptId] = new CaptureBuffer();
    }

    /// <summary>
    /// Returns true if capture is active for the given script.
    /// </summary>
    public bool IsCapturing(Guid scriptId) => _buffers.ContainsKey(scriptId);

    /// <summary>
    /// Called by the output writer to buffer output during an active capture.
    /// </summary>
    public void BufferOutput(Guid scriptId, ScriptOutput output)
    {
        if (!_buffers.TryGetValue(scriptId, out var buffer)) return;

        lock (buffer)
        {
            if (output.Kind == ScriptOutputKind.Error)
            {
                buffer.Errors.Add(output.Body ?? string.Empty);
            }
            else
            {
                buffer.Output.Add(output);
            }
        }
    }

    /// <summary>
    /// Gets captured output for a script. If <paramref name="wait"/> is true, blocks until the run completes.
    /// Evicts the buffer after returning.
    /// </summary>
    public async Task<HeadlessRunResult> GetCapturedOutputAsync(
        Guid scriptId, bool wait, int? timeoutMs, CancellationToken cancellationToken)
    {
        if (!_buffers.TryGetValue(scriptId, out var buffer))
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
                await buffer.CompletionSource.Task.WaitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                return BuildResult(scriptId, buffer, HeadlessRunResult.StatusTimeout);
            }
            catch (OperationCanceledException)
            {
                return BuildResult(scriptId, buffer, HeadlessRunResult.StatusCancelled);
            }
        }

        var statusOverride = buffer.CompletionSource.Task.IsCompletedSuccessfully
            ? null
            : HeadlessRunResult.StatusPending;
        return BuildResult(scriptId, buffer, statusOverride);
    }

    private HeadlessRunResult BuildResult(Guid scriptId, CaptureBuffer buffer, string? statusOverride)
    {
        // Once the buffer is retrieved it is evicted
        _buffers.TryRemove(scriptId, out _);

        List<ScriptOutput> output;
        List<string> errors;
        string finalStatus;
        double durationMs;

        lock (buffer)
        {
            output = new List<ScriptOutput>(buffer.Output);
            errors = new List<string>(buffer.Errors);
            finalStatus = statusOverride ?? buffer.FinalStatus ?? HeadlessRunResult.StatusCompleted;
            durationMs = buffer.DurationMs;
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

    private Task OnEnvironmentPropertyChanged(EnvironmentPropertyChangedEvent ev)
    {
        if (!_buffers.TryGetValue(ev.ScriptId, out var buffer)) return Task.CompletedTask;

        if (ev.PropertyName == nameof(ScriptEnvironment.RunDurationMilliseconds) && ev.NewValue is double durationMs)
        {
            lock (buffer)
            {
                buffer.DurationMs = durationMs;
            }
        }
        else if (ev.PropertyName == nameof(ScriptEnvironment.Status) && ev.NewValue is ScriptStatus newStatus)
        {
            if (newStatus is ScriptStatus.Ready or ScriptStatus.Error)
            {
                lock (buffer)
                {
                    buffer.FinalStatus = newStatus == ScriptStatus.Error
                        ? HeadlessRunResult.StatusFailed
                        : HeadlessRunResult.StatusCompleted;
                }

                buffer.CompletionSource.TrySetResult();
            }
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _evictionTimer.Stop();
        _evictionTimer.Dispose();
        _buffers.Clear();
    }

    private void EvictStaleBuffers()
    {
        var cutoff = DateTime.UtcNow - _bufferTtl;
        foreach (var (scriptId, buffer) in _buffers)
        {
            if (buffer.CreatedAt < cutoff)
            {
                _buffers.TryRemove(scriptId, out _);
            }
        }
    }

    private class CaptureBuffer
    {
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public List<ScriptOutput> Output { get; } = [];
        public List<string> Errors { get; } = [];
        public string? FinalStatus { get; set; }
        public double DurationMs { get; set; }
        public TaskCompletionSource CompletionSource { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
