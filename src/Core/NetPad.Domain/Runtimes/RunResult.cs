namespace NetPad.Runtimes
{
    public class RunResult
    {

        public RunResult(bool isRunAttemptSuccessful, bool isScriptRunSuccessful, double durationMs)
        {
            IsRunAttemptSuccessful = isRunAttemptSuccessful;
            IsScriptRunSuccessful = isScriptRunSuccessful;
            DurationMs = durationMs;
        }

        /// <summary>
        /// Indicates whether the operation to run the script occurred successfully.
        /// </summary>
        public bool IsRunAttemptSuccessful { get; }

        /// <summary>
        /// Indicates whether the actual script code ran successfully.
        /// </summary>
        public bool IsScriptRunSuccessful { get; }

        /// <summary>
        /// The duration, in milliseconds, it took to run the script code.
        /// </summary>
        public double DurationMs { get; }

        public static RunResult FailedToRun() => new RunResult(false, false, 0);
        public static RunResult Success(double durationMs) => new RunResult(true, true, durationMs);
    }
}
