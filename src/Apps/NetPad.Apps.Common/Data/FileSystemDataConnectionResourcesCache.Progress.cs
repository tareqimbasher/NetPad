using Microsoft.Extensions.Logging;
using NetPad.Data;
using NetPad.Events;

namespace NetPad.Apps.Data;

public sealed partial class FileSystemDataConnectionResourcesCache
{
    /// <summary>
    /// Tracks the stage of one connection and framework's resource generation and passes each stage to every caller
    /// waiting on it.
    /// </summary>
    /// <remarks>
    /// Callbacks run while the lock is held. That is what keeps a caller that joins mid-generation from seeing the
    /// current stage after a later one, and it is why callbacks must return quickly and never block.
    /// </remarks>
    private sealed class ResourceGenerationProgress(ILogger logger)
    {
        private readonly object _lock = new();
        private readonly List<Action<DataConnectionSetupStage>> _callbacks = [];
        private DataConnectionSetupStage? _currentStage;

        /// <summary>
        /// Adds a callback that receives every later stage, after first receiving the current stage if a generation
        /// is in progress. Dispose the result to remove it.
        /// </summary>
        public IDisposable AddCallback(Action<DataConnectionSetupStage> callback)
        {
            lock (_lock)
            {
                _callbacks.Add(callback);

                if (_currentStage is { } stage)
                {
                    TryReportStage(callback, stage, logger);
                }
            }

            return new DisposableToken(() => RemoveCallback(callback));
        }

        public void ReportStage(DataConnectionSetupStage stage)
        {
            lock (_lock)
            {
                _currentStage = stage;

                foreach (var callback in _callbacks.ToArray())
                {
                    TryReportStage(callback, stage, logger);
                }
            }
        }

        public void GenerationEnded()
        {
            lock (_lock)
            {
                _currentStage = null;
            }
        }

        private void RemoveCallback(Action<DataConnectionSetupStage> callback)
        {
            lock (_lock)
            {
                _callbacks.Remove(callback);
            }
        }
    }

    private static void TryReportStage(
        Action<DataConnectionSetupStage>? reportStage,
        DataConnectionSetupStage stage,
        ILogger logger)
    {
        if (reportStage == null)
        {
            return;
        }

        try
        {
            reportStage(stage);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error reporting data connection setup stage {Stage}", stage);
        }
    }
}
