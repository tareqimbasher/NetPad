using System.Threading.Tasks;

namespace NetPad.Runtimes
{
    /// <summary>
    /// A way to write output from the query runtime.
    /// </summary>
    public interface IQueryRuntimeOutputWriter
    {
        Task WriteAsync(object? output);
    }
}
