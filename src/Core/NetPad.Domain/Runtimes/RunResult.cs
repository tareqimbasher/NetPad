namespace NetPad.Runtimes;

/// <summary>
/// Represents the result of running a script.
/// </summary>
public class RunResult
{
    public RunResult(bool isRunAttemptSuccessful, bool isScriptCompletedSuccessfully, double durationMs)
    {
        IsRunAttemptSuccessful = isRunAttemptSuccessful;
        IsScriptCompletedSuccessfully = isScriptCompletedSuccessfully;
        DurationMs = durationMs;
    }

    /// <summary>
    /// Indicates whether the operation to run the script occurred successfully.
    /// </summary>
    public bool IsRunAttemptSuccessful { get; }

    /// <summary>
    /// Indicates whether the script code ran to completion successfully.
    /// </summary>
    public bool IsScriptCompletedSuccessfully { get; }

    /// <summary>
    /// The duration, in milliseconds, it took to run the script.
    /// </summary>
    public double DurationMs { get; }

    /// <summary>
    /// Returns a <see cref="RunResult"/> that indicates that the attempt to run the script failed.
    /// </summary>
    public static RunResult RunAttemptFailure() => new(false, false, 0);

    /// <summary>
    /// Returns a <see cref="RunResult"/> that indicates that the script ran but did not complete successfully.
    /// </summary>
    public static RunResult ScriptCompletionFailure(double durationMs) => new(true, false, durationMs);

    /// <summary>
    /// Returns a <see cref="RunResult"/> that indicates that the script ran and completed successfully.
    /// </summary>
    public static RunResult Success(double durationMs) => new(true, true, durationMs);
}
