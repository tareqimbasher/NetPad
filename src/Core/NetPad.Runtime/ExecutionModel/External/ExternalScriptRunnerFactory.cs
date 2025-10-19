using Microsoft.Extensions.DependencyInjection;
using NetPad.Scripts;

namespace NetPad.ExecutionModel.External;

public class ExternalScriptRunnerFactory(IServiceProvider serviceProvider)
    : IScriptRunnerFactory
{
    public IScriptRunner CreateRunner(Script script)
    {
        return ActivatorUtilities.CreateInstance<ExternalScriptRunner>(serviceProvider, script);
    }
}
