using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Compilation;
using NetPad.IO;
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

    public Task<IScriptRuntime<IScriptOutputAdapter<ScriptOutput, ScriptOutput>>> CreateScriptRuntimeAsync(Script script)
    {
        var runtime = new InMemoryScriptRuntime(
            script,
            _serviceProvider.CreateScope(),
            _serviceProvider.GetRequiredService<ICodeParser>(),
            _serviceProvider.GetRequiredService<ICodeCompiler>(),
            _serviceProvider.GetRequiredService<IPackageProvider>(),
            _serviceProvider.GetRequiredService<ILogger<InMemoryScriptRuntime>>()
        );

        return Task.FromResult<IScriptRuntime<IScriptOutputAdapter<ScriptOutput, ScriptOutput>>>(runtime);
    }
}
