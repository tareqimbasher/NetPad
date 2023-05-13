using System.Threading.Tasks;

namespace NetPad.IO;

/// <summary>
/// Reads input.
/// </summary>
public interface IInputReader<TInput>
{
    /// <summary>
    /// Read input.
    /// </summary>
    Task<TInput?> ReadAsync();
}
