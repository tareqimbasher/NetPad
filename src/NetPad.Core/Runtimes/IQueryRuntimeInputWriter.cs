using System.Threading.Tasks;

namespace NetPad.Runtimes
{
    /// <summary>
    /// A way to write input to the query runtime when a query requests user input.
    /// </summary>
    public interface IQueryRuntimeInputWriter
    {
        Task<string> ReadAsync();
    }
}