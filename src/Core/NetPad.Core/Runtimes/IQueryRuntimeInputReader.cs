using System.Threading.Tasks;

namespace NetPad.Runtimes
{
    /// <summary>
    /// A way to read input from a query runtime.
    /// </summary>
    public interface IQueryRuntimeInputReader
    {
        Task<string> ReadAsync();
    }
}
