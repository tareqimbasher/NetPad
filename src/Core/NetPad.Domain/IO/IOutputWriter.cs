using System.Threading.Tasks;

namespace NetPad.IO;

/// <summary>
/// Writes output.
/// </summary>
public interface IOutputWriter<in TOutput>
{
    /// <summary>
    /// Write output.
    /// </summary>
    /// <param name="output">The output to write.</param>
    /// <param name="title">A title to associate with the output.</param>
    Task WriteAsync(TOutput? output, string? title = null);
}
