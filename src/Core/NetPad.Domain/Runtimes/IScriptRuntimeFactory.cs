using System.Threading.Tasks;
using NetPad.IO;
using NetPad.Scripts;

namespace NetPad.Runtimes;

public interface IScriptRuntimeFactory
{
    Task<IScriptRuntime<IScriptOutputAdapter<ScriptOutput, ScriptOutput>>> CreateScriptRuntimeAsync(Script script);
}
