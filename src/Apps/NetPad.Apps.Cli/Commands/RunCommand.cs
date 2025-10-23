using System.CommandLine;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.ExecutionModel;
using NetPad.ExecutionModel.External;
using NetPad.IO;
using NetPad.Presentation;
using NetPad.Scripts;
using NetPad.Utilities;
using Spectre.Console;

namespace NetPad.Apps.Cli.Commands;

enum OutputFormat
{
    Console = 0,
    Text,
    Html,
    HtmlDoc
}

public static class RunCommand
{
    private sealed record Options(
        string? PathOrName,
        ScriptKind ScriptKind,
        OptimizationLevel OptimizationLevel,
        DataConnection? DataConnection,
        bool NoCache,
        bool ForceRebuild,
        bool Verbose,
        List<string> ScriptArgs,
        OutputFormat OutputFormat);

    public static void AddRunCommand(this RootCommand parent, IServiceProvider serviceProvider)
    {
        var runCmd = new Command(
            "run",
            "Run a script or a plain-text file.");
        parent.Subcommands.Add(runCmd);

        var pathOrNameArg = new Argument<string>("PATH|NAME")
        {
            Description =
                "A path to a script or plain-text file, or a name (or partial name) to search for in your script library.\n" +
                "If omitted, or if multiple matches are found, you’ll be prompted to choose from a list.",
            Arity = ArgumentArity.ZeroOrOne,
            HelpName = "PATH|NAME"
        };

        var connectionOption = new Option<string?>("--connection")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "The name of the database connection to use.",
            HelpName = "name"
        };

        var optimizeOption = new Option<bool>("--optimize")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Enable compiler optimizations."
        };

        var formatOption = new Option<OutputFormat>("--format")
        {
            Arity = ArgumentArity.ZeroOrOne,
            HelpName = "text|html|htmldoc",
            Description =
                "The format of script output. If not specified, will emit structured console output (default).\n" +
                "Values:\n" +
                "  - text       Plain text format; useful when piping to a file\n" +
                "  - html       HTML fragments\n" +
                "  - htmldoc    A complete HTML document",
        };

        var minimalOption = new Option<bool>("--minimal")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "If possible, use more minimal output formatting.",
        };

        var noCacheOption = new Option<bool>("--no-cache")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description =
                "Skip the build cache; do not use a cached build, if one exists, and do not cache the build from this run.",
        };

        var forceRebuildOption = new Option<bool>("--rebuild")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Rebuild even if a cached build exists. Replaces the current cached build, if any.",
        };

        var verboseOption = new Option<bool>("--verbose")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Be verbose.",
        };

        runCmd.Arguments.Add(pathOrNameArg);
        runCmd.Options.Add(connectionOption);
        runCmd.Options.Add(optimizeOption);
        runCmd.Options.Add(formatOption);
        runCmd.Options.Add(minimalOption);
        runCmd.Options.Add(noCacheOption);
        runCmd.Options.Add(forceRebuildOption);
        runCmd.Options.Add(verboseOption);
        runCmd.SetAction(async p =>
        {
            // Resolve the target connection
            var connectionName = p.GetValue(connectionOption);
            DataConnection? connection = null;
            if (!string.IsNullOrWhiteSpace(connectionName))
            {
                connection = await GetConnectionByNameAsync(connectionName, serviceProvider);
                if (connection == null)
                {
                    return 1;
                }
            }

            var scriptArgs = new List<string>();

            var options = new Options(
                p.GetValue(pathOrNameArg),
                ScriptKind.Program,
                p.GetValue(optimizeOption) ? OptimizationLevel.Release : OptimizationLevel.Debug,
                connection,
                p.GetValue(noCacheOption),
                p.GetValue(forceRebuildOption),
                p.GetValue(verboseOption),
                scriptArgs,
                p.GetValue(formatOption)
            );

            if (options.NoCache && options.ForceRebuild)
            {
                Presenter.Error($"Cannot use {noCacheOption.Name} and {forceRebuildOption.Name} at the same time.");
                return 1;
            }

            // Forward some args to script
            if (options.OutputFormat == OutputFormat.Text)
            {
                options.ScriptArgs.Add("-text");
            }

            if (options.OutputFormat == OutputFormat.Html)
            {
                options.ScriptArgs.Add("-html");
            }

            if (options.OutputFormat == OutputFormat.HtmlDoc)
            {
                options.ScriptArgs.Add("-html-msg");
            }

            if (p.GetValue(minimalOption))
            {
                options.ScriptArgs.Add("-minimal");
            }

            if (p.GetValue(verboseOption))
            {
                options.ScriptArgs.Add("-verbose");
            }

            scriptArgs.AddRange(p.UnmatchedTokens);

            return await ExecuteAsync(options, serviceProvider);
        });
    }

    private static async Task<DataConnection?> GetConnectionByNameAsync(string connectionName,
        IServiceProvider serviceProvider)
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

    private static async Task<int> ExecuteAsync(Options options, IServiceProvider serviceProvider)
    {
        var selectedScriptPath = SelectScript(serviceProvider, options);
        if (selectedScriptPath == null) return 1;

        var script = await LoadScriptAsync(serviceProvider, selectedScriptPath, options);
        if (script == null) return 1;

        ApplyOptions(script, options, serviceProvider);

        return await RunScriptAsync(serviceProvider, script, options);
    }

    private static string? SelectScript(IServiceProvider serviceProvider, Options options)
    {
        var settings = serviceProvider.GetRequiredService<Settings>();
        var matches = ScriptFinder.FindMatches(settings.ScriptsDirectoryPath, options.PathOrName);
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
                return Presenter.GetScriptPathMarkup(trimmed, options.PathOrName);
            };

            selectedScriptFilePath = AnsiConsole.Prompt(selection);
        }

        return selectedScriptFilePath;
    }

    private static async Task<Script?> LoadScriptAsync(
        IServiceProvider serviceProvider,
        string scriptFilePath,
        Options options)
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
            if (options.Verbose)
            {
                Presenter.Warn(
                    $"Could not load file as a normal .netpad file; assuming contents are code. " +
                    $"Reason: {ex.Message}");
            }
        }

        // If the normal way failed, assume the contents of the file is C# code and create a new script
        script ??= CreateScriptFromFile(serviceProvider, scriptFilePath);

        return script;
    }

    private static Script? CreateScriptFromFile(IServiceProvider serviceProvider, string scriptPath)
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

    private static void ApplyOptions(Script script, Options options, IServiceProvider serviceProvider)
    {
        script.Config.SetKind(options.ScriptKind);
        script.Config.SetOptimizationLevel(options.OptimizationLevel);

        if (options.DataConnection != null)
        {
            script.SetDataConnection(options.DataConnection);
        }
    }

    private static async Task<int> RunScriptAsync(IServiceProvider serviceProvider, Script script, Options options)
    {
        if (options.Verbose) Presenter.Info("Starting run...");

        bool redirectScriptProcessIo = options.OutputFormat == OutputFormat.HtmlDoc;
        var htmlDocumentOutput = options.OutputFormat == OutputFormat.HtmlDoc ? new StringBuilder() : null;

        // Create a script runner
        using var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();
        var scriptRunnerFactory = scope.ServiceProvider.GetRequiredService<IScriptRunnerFactory>();
        var scriptRunner = scriptRunnerFactory.CreateRunner(script);

        // Handle script & runner output
        scriptRunner.AddOutput(new ActionOutputWriter<object>((o, _) =>
        {
            if (htmlDocumentOutput != null && o is HtmlResultsScriptOutput htmlScriptOutput)
            {
                htmlDocumentOutput.Append(htmlScriptOutput.Body);
                return;
            }

            // If the script process outputs to STDOUT directly it prints to the console directly.
            // But errors might occur before the script is run, ie: compilation errors. In that
            // case the script runner will emit those errors using this output handler.
            if (!redirectScriptProcessIo && o is ScriptOutput error)
            {
                Presenter.Error(error.Body?.ToString() ?? "An error occured.");
                return;
            }

            if (o is ScriptOutput scriptOutput)
            {
                Console.WriteLine(scriptOutput.Body);
            }

            Console.WriteLine(o);
        }));

        // Configure run options
        var runOptions = new RunOptions();
        runOptions.SetOption(new ExternalScriptRunnerOptions
        {
            NoCache = options.NoCache,
            ForceRebuild = options.ForceRebuild,
            ProcessCliArgs = options.ScriptArgs.ToArray(),
            RedirectIo = redirectScriptProcessIo
        });

        await scriptRunner.RunScriptAsync(runOptions);

        if (htmlDocumentOutput != null)
        {
            var styles = AssemblyUtil.ReadEmbeddedResource(typeof(RunCommand).Assembly, "Assets.styles.css");
            var html = $"""
                        <!DOCTYPE html>
                        <html>
                        <head>
                          <meta charset="utf-8" />
                          <meta name="viewport" content="width=device-width" />
                          <style>{styles}</style>
                        </head>
                        <body>
                        {htmlDocumentOutput}
                        </body>
                        </html>
                        """;

            Console.WriteLine(html);
        }

        return 0;
    }
}
