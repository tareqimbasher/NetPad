namespace NetPad.IO;

/// <summary>
/// Reads input.
/// </summary>
public interface IInputReader<TInput>
{
    /// <summary>
    /// Read and return input.
    /// </summary>
    Task<TInput?> ReadAsync();
}
