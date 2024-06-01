using NetPad.Scripts;

namespace NetPad.ExecutionModel;

public interface IScriptRunnerFactory
{
    IScriptRunner CreateRunner(Script script);
}
