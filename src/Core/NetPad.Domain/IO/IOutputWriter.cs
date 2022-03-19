using System.Threading.Tasks;

namespace NetPad.IO
{
    /// <summary>
    /// Writes output.
    /// </summary>
    public interface IOutputWriter
    {
        Task WriteAsync(object? output, string? title = null);
    }
}
