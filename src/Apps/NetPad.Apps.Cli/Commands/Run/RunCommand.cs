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

namespace NetPad.Apps.Cli.Commands.Run;

public static class RunCommand
{
    private sealed record Options(
        string? PathOrName,
        ScriptKind ScriptKind,
        OptimizationLevel OptimizationLevel,
        Guid? DataConnectionId,
        bool NoCache,
        bool ForceRebuild,
        bool Verbose,
        List<string> ScriptArgs,
        bool OutputHtmlDocument);

    public static void AddRunCommand(this RootCommand parent, IServiceProvider serviceProvider)
    {
        var runCmd = new Command(
            "run",
            "Run a script. Script builds are cached for faster executions.");
        parent.Subcommands.Add(runCmd);

        var pathOrNameArg = new Argument<string>("PATH|NAME")
        {
            Description =
                "A path to a script file, or a name (or partial name) to search for in your script library.\n" +
                "If omitted, or if multiple matches are found, you’ll be prompted to choose from a list.",
            Arity = ArgumentArity.ZeroOrOne,
            HelpName = "PATH|NAME"
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

        var consoleOption = new Option<bool>("--console")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Print output as console formatted text.",
        };

        var noColorOption = new Option<bool>("--no-color")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Don't use ANSI colors when printing output to console. Useful for then piping to a file.",
        };

        var htmlOption = new Option<bool>("--html")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Print output in raw HTML format.",
        };

        var htmlDocOption = new Option<bool>("--html-doc")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Print output as a HTML document.",
        };

        var htmlMessageOption = new Option<bool>("--html-msg")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description =
                "Print output in envelope message format where the body is raw HTML. For inter-process communication use.",
        };

        var verboseOption = new Option<bool>("--verbose")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Be verbose.",
        };

        runCmd.Arguments.Add(pathOrNameArg);
        runCmd.Options.Add(noCacheOption);
        runCmd.Options.Add(forceRebuildOption);
        runCmd.Options.Add(consoleOption);
        runCmd.Options.Add(noColorOption);
        runCmd.Options.Add(htmlOption);
        runCmd.Options.Add(htmlDocOption);
        runCmd.Options.Add(htmlMessageOption);
        runCmd.Options.Add(verboseOption);
        runCmd.SetAction(async p =>
        {
            var scriptArgs = new List<string>();
            var options = new Options(
                p.GetValue(pathOrNameArg),
                ScriptKind.Program,
                OptimizationLevel.Debug,
                null,
                p.GetValue(noCacheOption),
                p.GetValue(forceRebuildOption),
                p.GetValue(verboseOption),
                scriptArgs,
                p.GetValue(htmlDocOption)
            );

            // Some args should be forwarded to the script
            if (p.GetValue(consoleOption))
            {
                options.ScriptArgs.Add("-console");
            }

            if (p.GetValue(noColorOption))
            {
                options.ScriptArgs.Add("-no-color");
            }

            if (p.GetValue(htmlOption))
            {
                options.ScriptArgs.Add("-html");
            }

            if (p.GetValue(htmlMessageOption) || p.GetValue(htmlDocOption))
            {
                options.ScriptArgs.Add("-html-msg");
            }

            if (p.GetValue(verboseOption))
            {
                options.ScriptArgs.Add("-verbose");
            }

            scriptArgs.AddRange(p.UnmatchedTokens);

            return await ExecuteAsync(options, serviceProvider);
        });

        runCmd.AddRunCacheCommand(serviceProvider);
    }

    private static async Task<int> ExecuteAsync(Options options, IServiceProvider serviceProvider)
    {
        var selectedScriptPath = SelectScript(serviceProvider, options);
        if (selectedScriptPath == null) return 1;

        var script = await LoadScriptAsync(serviceProvider, selectedScriptPath, options);
        if (script == null) return 1;

        await ApplyOptionsAsync(script, options, serviceProvider);

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

    private static async Task ApplyOptionsAsync(Script script, Options options, IServiceProvider serviceProvider)
    {
        script.Config.SetKind(options.ScriptKind);
        script.Config.SetOptimizationLevel(options.OptimizationLevel);

        if (options.DataConnectionId.HasValue)
        {
            var dataConnectionRepository = serviceProvider.GetRequiredService<IDataConnectionRepository>();
            var connection = await dataConnectionRepository.GetAsync(options.DataConnectionId.Value);

            if (connection == null)
            {
                Presenter.Error("Could not find a data connection with the specified id.");
            }
            else
            {
                script.SetDataConnection(connection);
            }
        }
    }

    private static async Task<int> RunScriptAsync(IServiceProvider serviceProvider, Script script, Options options)
    {
        if (options.Verbose) Presenter.Info("Starting run...");

        bool redirectScriptProcessIo = options.OutputHtmlDocument;
        var htmlDocumentOutput = options.OutputHtmlDocument ? new StringBuilder() : null;

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
