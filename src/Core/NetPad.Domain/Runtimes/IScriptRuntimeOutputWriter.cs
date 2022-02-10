using System.Threading.Tasks;

namespace NetPad.Runtimes
{
    /// <summary>
    /// A way to write output from the script runtime.
    /// </summary>
    public interface IScriptRuntimeOutputWriter
    {
        Task WriteAsync(object? output, string? title = null);
    }
}
