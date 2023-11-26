using NetPad.Scripts;

namespace NetPad.Runtimes;

public interface IScriptRuntimeFactory
{
    IScriptRuntime CreateScriptRuntime(Script script);
}
