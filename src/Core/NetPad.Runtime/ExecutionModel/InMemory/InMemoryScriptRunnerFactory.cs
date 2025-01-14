using Microsoft.Extensions.DependencyInjection;
using NetPad.Scripts;

namespace NetPad.ExecutionModel.InMemory;

public class InMemoryScriptRunnerFactory(IServiceProvider serviceProvider) : IScriptRunnerFactory
{
    public IScriptRunner CreateRunner(Script script)
    {
#pragma warning disable CS0618 // Type or member is obsolete
        return ActivatorUtilities.CreateInstance<InMemoryScriptRunner>(serviceProvider, script);
#pragma warning restore CS0618 // Type or member is obsolete
    }
}
