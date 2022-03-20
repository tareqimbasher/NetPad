using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Compilation;
using NetPad.Packages;
using NetPad.Scripts;

namespace NetPad.Runtimes;

public class DefaultExternalProcessScriptRuntimeFactory : IScriptRuntimeFactory
{
    private readonly IServiceProvider _serviceProvider;

    public DefaultExternalProcessScriptRuntimeFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task<IScriptRuntime> CreateScriptRuntimeAsync(Script script)
    {
        var runtime = new ExternalProcessScriptRuntime(
            script,
            _serviceProvider.CreateScope(),
            _serviceProvider.GetRequiredService<ICodeParser>(),
            _serviceProvider.GetRequiredService<ICodeCompiler>(),
            _serviceProvider.GetRequiredService<IPackageProvider>(),
            _serviceProvider.GetRequiredService<ILogger<ExternalProcessScriptRuntime>>()
        );

        return Task.FromResult<IScriptRuntime>(runtime);
    }
}
