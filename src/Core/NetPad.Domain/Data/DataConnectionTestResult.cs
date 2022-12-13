using System;

namespace NetPad.Data;

public class DataConnectionTestResult
{
    public DataConnectionTestResult(bool success)
    {
        Success = success;
    }

    public DataConnectionTestResult(bool success, string message) : this(success)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }

    public bool Success { get; }
    public string? Message { get; }
}
