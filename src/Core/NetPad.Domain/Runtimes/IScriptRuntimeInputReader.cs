using System.Threading.Tasks;

namespace NetPad.Runtimes
{
    /// <summary>
    /// A way to read input from a script runtime.
    /// </summary>
    public interface IScriptRuntimeInputReader
    {
        Task<string> ReadAsync();
    }
}
