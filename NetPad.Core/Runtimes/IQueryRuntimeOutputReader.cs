using System.Threading.Tasks;

namespace NetPad.Runtimes
{
    public interface IQueryRuntimeOutputReader
    {
        Task ReadAsync(object? output);
    }
}