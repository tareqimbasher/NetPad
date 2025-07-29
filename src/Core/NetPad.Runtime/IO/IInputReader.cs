namespace NetPad.IO;

/// <summary>
/// Requests input.
/// </summary>
public interface IInputReader<TInput>
{
    /// <summary>
    /// Read and return input.
    /// </summary>
    Task<TInput?> ReadAsync();
}
