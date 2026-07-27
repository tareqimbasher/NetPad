namespace NetPad.Data;

/// <summary>
/// The outcome of opening a connection to a data source.
/// </summary>
/// <param name="Success">Whether the connection test succeeded or failed.</param>
/// <param name="Message">The error message if the test failed.</param>
/// <param name="ServerVersion">The version the server reported when the connection was opened, if it reports one.</param>
public record DataConnectionTestResult(bool Success, string? Message, string? ServerVersion)
{
    public static DataConnectionTestResult Succeeded(string? serverVersion) => new(true, null, serverVersion);

    public static DataConnectionTestResult Failed(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return new DataConnectionTestResult(false, message, null);
    }
}
