using Microsoft.Extensions.Logging;
using NetPad.Application;
using NetPad.Compilation;
using NetPad.Configuration;
using NetPad.Data.Metadata;
using NetPad.DotNet;
using NetPad.ExecutionModel;
using NetPad.ExecutionModel.External;
using NetPad.Packages;
using NetPad.Scripts;

namespace NetPad.Services;

/// <summary>
/// Creates <see cref="ExternalScriptRunner"/> instances for headless (non-GUI) script execution.
/// The main app uses ClientServer model, this factory bypasses that to use the External model directly.
/// </summary>
public class HeadlessScriptRunnerFactory(
    ICodeCompiler codeCompiler,
    IPackageProvider packageProvider,
    IAppStatusMessagePublisher appStatusMessagePublisher,
    IDataConnectionResourcesCache dataConnectionResourcesCache,
    IDotNetInfo dotNetInfo,
    Settings settings,
    ILoggerFactory loggerFactory)
{
    public IScriptRunner CreateRunner(Script script)
    {
        var codeParser = new ExternalRunnerCSharpCodeParser();
        var logger = loggerFactory.CreateLogger<ExternalScriptRunner>();

        return new ExternalScriptRunner(
            script,
            codeParser,
            codeCompiler,
            packageProvider,
            appStatusMessagePublisher,
            dataConnectionResourcesCache,
            dotNetInfo,
            settings,
            logger);
    }
}
