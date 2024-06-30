using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetPad.Compilation;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.Packages;
using NetPad.Scripts;

namespace NetPad.ExecutionModel.External;

public class ExternalScriptRunnerFactory(IServiceProvider serviceProvider, ExternalScriptRunnerOptions options)
    : IScriptRunnerFactory
{
    public IScriptRunner CreateRunner(Script script)
    {
        return new ExternalScriptRunner(
            options,
            script,
            serviceProvider.GetRequiredService<ICodeParser>(),
            serviceProvider.GetRequiredService<ICodeCompiler>(),
            serviceProvider.GetRequiredService<IPackageProvider>(),
            serviceProvider.GetRequiredService<IDataConnectionResourcesCache>(),
            serviceProvider.GetRequiredService<IDotNetInfo>(),
            serviceProvider.GetRequiredService<Settings>(),
            serviceProvider.GetRequiredService<ILogger<ExternalScriptRunner>>()
        );
    }
}
