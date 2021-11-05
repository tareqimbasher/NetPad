using System.Threading.Tasks;

namespace NetPad.Runtimes
{
    /// <summary>
    /// A way to read output from the query runtime when a query outputs data.
    /// </summary>
    public interface IQueryRuntimeOutputReader
    {
        Task ReadAsync(object? output);
    }
}