using Microsoft.Extensions.DependencyInjection;
using NetPad.Scripts;

namespace NetPad.ExecutionModel.ClientServer;

public class ClientServerScriptRunnerFactory(IServiceProvider serviceProvider) : IScriptRunnerFactory
{
    public IScriptRunner CreateRunner(Script script)
    {
        return ActivatorUtilities.CreateInstance<ClientServerScriptRunner>(serviceProvider, script);
    }
}
