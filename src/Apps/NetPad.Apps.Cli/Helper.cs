using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.Exceptions;
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
    public static string? SelectScript(IServiceProvider serviceProvider, string? pathOrName, string action)
    {
        var settings = serviceProvider.GetRequiredService<Settings>();
        var matches = ScriptFinder.FindMatches(settings.ScriptsDirectoryPath, pathOrName);
        if (matches.Length == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Did not match any known scripts[/]");
            return null;
        }

        var selectedScriptFilePath = matches[0];

        // If exactly one match has a base name that equals the query, auto-select it
        if (matches.Length > 1 && !string.IsNullOrWhiteSpace(pathOrName))
        {
            var exactNameMatches = matches
                .Where(m => Path.GetFileNameWithoutExtension(m)
                    .Equals(pathOrName, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (exactNameMatches.Length == 1)
                return exactNameMatches[0];
        }

        if (matches.Length > 1)
        {
            var scriptsDirPath = settings.ScriptsDirectoryPath;

            var selection = new SelectionPrompt<string>()
                .Title($"Which script do you want to [green]{action}[/]?")
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
        // First assume the script file is a valid .netpad file with the proper format and
        // load the script file the "normal" way
        try
        {
            var scriptRepository = serviceProvider.GetRequiredService<IScriptRepository>();
            return await scriptRepository.GetAsync(scriptFilePath);
        }
        catch (ScriptNotFoundException)
        {
            Presenter.Error($"Script file not found: {scriptFilePath}");
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Presenter.Error($"Could not read script file '{scriptFilePath}': {ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            if (verbose)
            {
                Presenter.Warn(
                    $"Could not load file as a .netpad file; treating contents as code. " +
                    $"Reason: {ex.Message}");
            }
        }

        // If the normal way failed, assume the contents of the file is C# code and create a new script
        return CreateScriptFromPlainTextFile(serviceProvider, scriptFilePath);
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

        string code;
        try
        {
            code = File.ReadAllText(scriptPath);
        }
        catch (FileNotFoundException)
        {
            Presenter.Error($"File not found: {scriptPath}");
            return null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Presenter.Error($"Could not read file '{scriptPath}': {ex.Message}");
            return null;
        }

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

    /// <summary>
    /// Parses a script kind string (case-insensitive). Returns false and emits an error
    /// if the value is provided but not recognized. A null input returns true with a null kind.
    /// </summary>
    public static bool TryParseScriptKind(string? kindStr, out ScriptKind? kind)
    {
        kind = null;
        if (kindStr == null)
        {
            return true;
        }

        if (kindStr.Equals("program", StringComparison.OrdinalIgnoreCase))
        {
            kind = ScriptKind.Program;
            return true;
        }

        if (kindStr.Equals("sql", StringComparison.OrdinalIgnoreCase))
        {
            kind = ScriptKind.SQL;
            return true;
        }

        Presenter.Error($"Unknown script kind '{kindStr}'. Supported values: program, sql.");
        return false;
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
        Path.TrimEndingDirectorySeparator(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

    /// <summary>
    /// If the path starts with the user's profile (home) directory, the replaces the part with a tilde (~).
    /// </summary>
    public static string? ShortenHomePath(string? path)
    {
        if (path == null)
        {
            return null;
        }

        if (!path.StartsWith(_homePath, StringComparison.InvariantCultureIgnoreCase))
        {
            return path;
        }

        return $"~{path[_homePath.Length..]}";
    }
}
