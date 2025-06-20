using NetPad.ExecutionModel;
using NetPad.Scripts;

namespace NetPad.Tests.Services;

public class NullScriptRunnerFactory : IScriptRunnerFactory
{
    public IScriptRunner CreateRunner(Script script)
    {
        return new NullScriptRunner();
    }
}
