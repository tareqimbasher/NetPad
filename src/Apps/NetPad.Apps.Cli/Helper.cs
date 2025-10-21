using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.Scripts;
using Spectre.Console;

namespace NetPad.Apps.Cli;

public static class Helper
{
    /// <summary>
    /// Given a path or name string, it will select the script that best matches the query.
    /// If multiple matches are found, the user will be asked to select from a list.
    /// If no matches are found, null is returned.
    /// </summary>
    public static string? SelectScript(IServiceProvider serviceProvider, string? pathOrName)
    {
        var settings = serviceProvider.GetRequiredService<Settings>();
        var matches = ScriptFinder.FindMatches(settings.ScriptsDirectoryPath, pathOrName);
        if (matches.Length == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Did not match any known scripts[/]");
            return null;
        }

        var selectedScriptFilePath = matches[0];

        if (matches.Length > 1)
        {
            var scriptsDirPath = settings.ScriptsDirectoryPath;

            var selection = new SelectionPrompt<string>()
                .Title("Which script do you want to [green]run[/]?")
                .PageSize(50)
                .MoreChoicesText("[grey](Move up and down to reveal more)[/]")
                .AddChoices(matches);

            selection.Converter = s =>
            {
                var trimmed = Path.GetRelativePath(scriptsDirPath, s);
                return Presenter.GetScriptPathMarkup(trimmed, pathOrName);
            };

            selectedScriptFilePath = AnsiConsole.Prompt(selection);
        }

        return selectedScriptFilePath;
    }

    /// <summary>
    /// Loads a script file from a specified path.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="scriptFilePath"></param>
    /// <param name="verbose"></param>
    /// <returns></returns>
    public static async Task<Script?> LoadScriptFileAsync(
        IServiceProvider serviceProvider,
        string scriptFilePath,
        bool verbose)
    {
        Script? script = null;

        // First assume the script file is a valid .netpad file with the proper format and
        // load the script file the "normal" way
        try
        {
            var scriptRepository = serviceProvider.GetRequiredService<IScriptRepository>();
            script = await scriptRepository.GetAsync(scriptFilePath);
        }
        catch (Exception ex)
        {
            if (verbose)
            {
                Presenter.Warn(
                    $"Could not load file as a normal .netpad file; assuming contents are code. " +
                    $"Reason: {ex.Message}");
            }
        }

        // If the normal way failed, assume the contents of the file is C# code and create a new script
        script ??= CreateScriptFromPlainTextFile(serviceProvider, scriptFilePath);

        return script;
    }

    /// <summary>
    /// Creates a <see cref="Script"/> from a plain text file on disk.
    /// </summary>
    /// <returns>The created script, or null if there was a problem.</returns>
    public static Script? CreateScriptFromPlainTextFile(IServiceProvider serviceProvider, string scriptPath)
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


    private static readonly string _homePath =
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).TrimEnd('/');

    /// <summary>
    /// If the path starts with the user's profile (home) directory, the replaces the part with a tilde (~).
    /// </summary>
    public static string? ShortenHomePath(string? path)
    {
        if (path == null)
        {
            return null;
        }

        var index = path.IndexOf(_homePath, StringComparison.InvariantCultureIgnoreCase);
        if (index < 0)
        {
            return path;
        }

        return $"~{path[_homePath.Length..]}";
    }
}
