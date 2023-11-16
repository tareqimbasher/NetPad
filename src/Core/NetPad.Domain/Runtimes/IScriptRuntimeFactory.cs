using System.Threading.Tasks;
using NetPad.Scripts;

namespace NetPad.Runtimes;

public interface IScriptRuntimeFactory
{
    Task<IScriptRuntime> CreateScriptRuntimeAsync(Script script);
}
