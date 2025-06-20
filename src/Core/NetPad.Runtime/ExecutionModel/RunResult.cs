namespace NetPad.ExecutionModel;

/// <summary>
/// Encapsulates the outcome of executing a script, including whether the run was initiated,
/// whether it completed successfully or was cancelled, and how long it ran.
/// </summary>
public class RunResult(
    bool isRunAttemptSuccessful,
    bool isScriptCompletedSuccessfully,
    bool isRunCancelled,
    double durationMs)
{
    /// <summary>
    /// Gets a value indicating whether the attempt to run the script was successfully initiated.
    /// </summary>
    /// <value>
    ///   <c>true</c> if the script run was started without setup or compilation errors; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    ///   A value of <c>false</c> means the script never began execution (for example, due to a compilation or setup failure).
    ///   When this is <c>false</c>, <see cref="DurationMs"/> will be zero.
    /// </remarks>
    public bool IsRunAttemptSuccessful { get; } = isRunAttemptSuccessful;

    /// <summary>
    /// Gets a value indicating whether the script ran to completion without error.
    /// </summary>
    /// <value>
    ///   <c>true</c> if the script finished execution normally (no unhandled exceptions); otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    ///   A return value of <c>false</c> means that execution ended due to an unhandled exception or was cancelled
    ///   before completion. To distinguish cancellations from errors, check <see cref="IsRunCancelled"/>.
    /// </remarks>
    public bool IsScriptCompletedSuccessfully { get; } = isScriptCompletedSuccessfully;

    /// <summary>
    /// Gets a value indicating whether the script run was cancelled before completion.
    /// </summary>
    /// <value>
    ///   <c>true</c> if the run was explicitly cancelled (for example, by a stop request); otherwise, <c>false</c>.
    /// </value>
    public bool IsRunCancelled { get; } = isRunCancelled;

    /// <summary>
    /// Gets the execution duration of the script, in milliseconds.
    /// </summary>
    /// <value>
    ///   The elapsed time in milliseconds that the script was running; zero if the script never ran or was cancelled early.
    /// </value>
    /// <remarks>
    ///   Includes only the actual script execution time, excluding setup or teardown overhead.
    ///   This value is only meaningful when <see cref="IsRunAttemptSuccessful"/> is <c>true</c>.
    /// </remarks>
    public double DurationMs { get; } = durationMs;

    /// <summary>
    /// Returns a <see cref="RunResult"/> indicating that the attempt to run the script failed.
    /// </summary>
    public static RunResult RunAttemptFailure() => new(false, false, false, 0);

    /// <summary>
    /// Returns a <see cref="RunResult"/> that indicating that the script ran but did not complete successfully.
    /// </summary>
    public static RunResult ScriptCompletionFailure(double durationMs) => new(true, false, false, durationMs);

    /// <summary>
    /// Returns a <see cref="RunResult"/> that indicating that the script run was cancelled either before or after it started.
    /// </summary>
    public static RunResult RunCancelled() => new(true, false, true, 0);

    /// <summary>
    /// Returns a <see cref="RunResult"/> that indicating that the script successfully ran to completion without any errors.
    /// </summary>
    public static RunResult Success(double durationMs) => new(true, true, false, durationMs);

    public override string ToString()
    {
        return
            $"IsRunAttemptSuccessful: {IsRunAttemptSuccessful}, IsScriptCompletedSuccessfully: {IsScriptCompletedSuccessfully}, IsRunCancelled: {IsRunCancelled}, DurationMs: {DurationMs}";
    }
}
