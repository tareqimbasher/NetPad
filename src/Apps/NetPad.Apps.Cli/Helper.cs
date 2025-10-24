using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.Scripts;

namespace NetPad.Apps.Cli;

public static class Helper
{
    /// <summary>
    /// Creates a <see cref="Script"/> from a file on disk.
    /// </summary>
    /// <returns>The created script, or null if there was a problem.</returns>
    public static Script? CreateScriptFromFile(IServiceProvider serviceProvider, string scriptPath)
    {
        var dotNetInfo = serviceProvider.GetRequiredService<IDotNetInfo>();
        var latestInstalledSdkVersion = dotNetInfo.GetLatestSupportedDotNetSdkVersion()?.GetFrameworkVersion();
        if (latestInstalledSdkVersion == null)
        {
            Presenter.Error("Could not find an installed .NET SDK.");
            return null;
        }

        var code = File.ReadAllText(scriptPath);
        var namespaces = ScriptConfigDefaults.DefaultNamespaces;
        var kind = ScriptKind.Program;
        var optimizationLevel = OptimizationLevel.Debug;
        var scriptId = ScriptIdGenerator.IdFromFilePath(scriptPath);

        var script = new Script(
            scriptId,
            Path.GetFileName(scriptPath),
            new ScriptConfig(
                kind,
                latestInstalledSdkVersion.Value,
                namespaces: namespaces,
                optimizationLevel: optimizationLevel
            ),
            code
        );

        script.SetPath(scriptPath);
        return script;
    }

    /// <summary>
    /// Creates a <see cref="Script"/> from a code string.
    /// </summary>
    /// <returns>The created script, or null if there was a problem.</returns>
    public static Script? CreateScriptFromCode(IServiceProvider serviceProvider, string code)
    {
        var dotNetInfo = serviceProvider.GetRequiredService<IDotNetInfo>();
        var latestInstalledSdkVersion = dotNetInfo.GetLatestSupportedDotNetSdkVersion()?.GetFrameworkVersion();
        if (latestInstalledSdkVersion == null)
        {
            Presenter.Error("Could not find an installed .NET SDK.");
            return null;
        }

        var namespaces = ScriptConfigDefaults.DefaultNamespaces;
        var kind = ScriptKind.Program;
        var optimizationLevel = OptimizationLevel.Debug;

        // "Inline" scripts will share a single script ID, which means they share one build cache
        // Multiple runs for the same code will reuse cache. If code, or script config/params, change
        // a new build will be initiated.
        var scriptId = ScriptIdGenerator.InlineScript;

        var script = new Script(
            scriptId,
            "Inline Script",
            new ScriptConfig(
                kind,
                latestInstalledSdkVersion.Value,
                namespaces: namespaces,
                optimizationLevel: optimizationLevel
            ),
            code
        );

        return script;
    }

    public static async Task<DataConnection?> GetConnectionByNameAsync(
        IServiceProvider serviceProvider,
        string connectionName)
    {
        var dataConnectionRepository = serviceProvider.GetRequiredService<IDataConnectionRepository>();
        var matches = (await dataConnectionRepository.GetAllAsync())
            .Where(x => x.Name.Equals(connectionName, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (matches.Length == 0)
        {
            Presenter.Error($"No connection with the name '{connectionName}' was found.");
            return null;
        }

        if (matches.Length > 1)
        {
            Presenter.Error($"More than one connection with the name '{connectionName}' was found.");
            return null;
        }

        return matches[0];
    }
}
