using Microsoft.Extensions.DependencyInjection;
using NetPad.Scripts;

namespace NetPad.ExecutionModel.External;

public class ExternalScriptRunnerFactory(IServiceProvider serviceProvider, ExternalScriptRunnerOptions options)
    : IScriptRunnerFactory
{
    public IScriptRunner CreateRunner(Script script)
    {
        return ActivatorUtilities.CreateInstance<ExternalScriptRunner>(serviceProvider, options, script);
    }
}
