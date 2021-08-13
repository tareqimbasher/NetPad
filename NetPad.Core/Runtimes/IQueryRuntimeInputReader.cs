using System.Threading.Tasks;

namespace NetPad.Runtimes
{
    public interface IQueryRuntimeInputReader
    {
        Task<string> ReadAsync();
    }
}