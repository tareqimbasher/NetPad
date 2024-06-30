namespace NetPad.Data;

public class DataConnectionTestResult(bool success)
{
    public DataConnectionTestResult(bool success, string message) : this(success)
    {
        Message = message ?? throw new ArgumentNullException(nameof(message));
    }

    public bool Success { get; } = success;
    public string? Message { get; }
}
