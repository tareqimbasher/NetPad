namespace NetPad.ExecutionModel;

/// <summary>
/// Represents the result of running a script.
/// </summary>
public class RunResult(
    bool isRunAttemptSuccessful,
    bool isScriptCompletedSuccessfully,
    bool isRunCancelled,
    double durationMs)
{
    /// <summary>
    /// Indicates whether the operation to run the script occurred successfully.
    /// </summary>
    public bool IsRunAttemptSuccessful { get; } = isRunAttemptSuccessful;

    /// <summary>
    /// Indicates whether the script code ran to completion successfully.
    /// </summary>
    public bool IsScriptCompletedSuccessfully { get; } = isScriptCompletedSuccessfully;

    /// <summary>
    /// Indicates the run was cancelled.
    /// </summary>
    public bool IsRunCancelled { get; } = isRunCancelled;

    /// <summary>
    /// The duration, in milliseconds, it took to run the script.
    /// </summary>
    public double DurationMs { get; } = durationMs;

    /// <summary>
    /// Returns a <see cref="RunResult"/> that indicates that the attempt to run the script failed.
    /// </summary>
    public static RunResult RunAttemptFailure() => new(false, false, false, 0);

    /// <summary>
    /// Returns a <see cref="RunResult"/> that indicates that the script ran but did not complete successfully.
    /// </summary>
    public static RunResult ScriptCompletionFailure(double durationMs) => new(true, false, false, durationMs);

    /// <summary>
    /// Returns a <see cref="RunResult"/> that indicates that the script ran but did not complete successfully.
    /// </summary>
    public static RunResult RunCancelled() => new(true, false, true, 0);

    /// <summary>
    /// Returns a <see cref="RunResult"/> that indicates that the script ran and completed successfully.
    /// </summary>
    public static RunResult Success(double durationMs) => new(true, true, false, durationMs);
}
