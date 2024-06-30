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
    /// <param name="cancellationToken">A cancellation token to cancel writing of output.</param>
    Task WriteAsync(TOutput? output, string? title = null, CancellationToken cancellationToken = default);
}
