using System.Collections.Generic;
using NetPad.Presentation;

namespace NetPad.Dtos;

public class HeadlessRunResult
{
    public const string StatusCompleted = "completed";
    public const string StatusFailed = "failed";
    public const string StatusTimeout = "timeout";
    public const string StatusCancelled = "cancelled";
    public const string StatusPending = "pending";

    /// <summary>
    /// The execution status: "completed", "failed", "timeout", or "cancelled".
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Whether the script executed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The execution duration in milliseconds.
    /// </summary>
    public double DurationMs { get; init; }

    /// <summary>
    /// The collected output from the script.
    /// </summary>
    public List<ScriptOutput> Output { get; init; } = [];

    /// <summary>
    /// Compilation errors, if any.
    /// </summary>
    public List<string>? CompilationErrors { get; init; }

    /// <summary>
    /// A runtime error message, if one occurred.
    /// </summary>
    public string? Error { get; init; }
}
