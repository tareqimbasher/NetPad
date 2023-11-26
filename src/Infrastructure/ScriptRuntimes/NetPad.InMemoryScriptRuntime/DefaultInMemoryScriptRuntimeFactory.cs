using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Compilation;
using NetPad.Packages;
using NetPad.Scripts;

namespace NetPad.Runtimes;

public class DefaultInMemoryScriptRuntimeFactory : IScriptRuntimeFactory
{
    private readonly IServiceProvider _serviceProvider;

    public DefaultInMemoryScriptRuntimeFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IScriptRuntime CreateScriptRuntime(Script script)
    {
        return new InMemoryScriptRuntime(
            script,
            _serviceProvider.CreateScope(),
            _serviceProvider.GetRequiredService<ICodeParser>(),
            _serviceProvider.GetRequiredService<ICodeCompiler>(),
            _serviceProvider.GetRequiredService<IPackageProvider>(),
            _serviceProvider.GetRequiredService<ILogger<InMemoryScriptRuntime>>()
        );
    }
}
