using Microsoft.Extensions.DependencyInjection;

namespace NetPad.Scripts;

public class DefaultScriptEnvironmentFactory : IScriptEnvironmentFactory
{
    private readonly IServiceProvider _serviceProvider;

    public DefaultScriptEnvironmentFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<ScriptEnvironment> CreateEnvironmentAsync(Script script)
    {
        var environment = new ScriptEnvironment(script, _serviceProvider.CreateScope());
        return Task.FromResult(environment);
    }
}
