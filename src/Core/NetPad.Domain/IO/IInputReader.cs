using System.Threading.Tasks;

namespace NetPad.IO
{
    /// <summary>
    /// Reads input.
    /// </summary>
    public interface IInputReader
    {
        Task<string?> ReadAsync();
    }
}
