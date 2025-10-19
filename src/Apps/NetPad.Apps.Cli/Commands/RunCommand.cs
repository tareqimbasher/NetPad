using System.CommandLine;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Configuration;
using NetPad.Data;
using NetPad.DotNet;
using NetPad.ExecutionModel;
using NetPad.IO;
using NetPad.Presentation;
using NetPad.Scripts;
using Spectre.Console;

namespace NetPad.Apps.Cli.Commands;

public static class RunCommand
{
    public sealed record Options(
        string PathOrName,
        ScriptKind ScriptKind,
        OptimizationLevel OptimizationLevel,
        Guid? DataConnectionId,
        bool Verbose);

    public static void AddRunCommand(this RootCommand parent, IServiceProvider serviceProvider)
    {
        var pathOrNameArg = new Argument<string>("script")
        {
            Description = "The script to run. If this matches more than 1 script, you will be prompted to select one.",
            Arity = ArgumentArity.ExactlyOne,
            HelpName = "PATH|NAME"
        };

        // Passthrough to running script
        var consoleOption = new Option<string>("-console")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Print output as console formatted text.",
        };

        // Passthrough to running script
        var noColorOption = new Option<string>("-no-color")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Print output without colors. Useful for then piping to a file.",
        };

        // Passthrough to running script
        var htmlOption = new Option<string>("-html")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Print output in HTML format. For internal use only.",
        };

        // Passthrough to running script
        var verboseOption = new Option<bool>("-verbose")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Be verbose.",
        };

        var runCmd = new Command("run", "Run a script by path or name.");
        parent.Subcommands.Add(runCmd);
        runCmd.Arguments.Add(pathOrNameArg);
        runCmd.Options.Add(consoleOption);
        runCmd.Options.Add(noColorOption);
        runCmd.Options.Add(htmlOption);
        runCmd.Options.Add(verboseOption);
        runCmd.SetAction(async p => await SelectScriptAsync(
            new Options(
                p.GetRequiredValue(pathOrNameArg),
                ScriptKind.Program,
                OptimizationLevel.Debug,
                null,
                p.GetRequiredValue(verboseOption)
            ),
            serviceProvider));
    }

    private static async Task<int> SelectScriptAsync(Options options, IServiceProvider serviceProvider)
    {
        var settings = serviceProvider.GetRequiredService<Settings>();
        var matches = ScriptFinder.FindMatches(settings.ScriptsDirectoryPath, options.PathOrName);
        if (matches.Length == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Did not match any known scripts[/]");
            return 1;
        }

        var selectedScriptFilePath = matches[0];

        if (matches.Length > 1)
        {
            var scriptsDirPath = settings.ScriptsDirectoryPath;

            var selection = new SelectionPrompt<string>()
                .Title("Which script do you want to [green]run[/]?")
                .PageSize(20)
                .MoreChoicesText("[grey](Move up and down to reveal more)[/]")
                .AddChoices(matches);

            selection.Converter = s =>
            {
                var trimmed = Path.GetRelativePath(scriptsDirPath, s);
                return Presenter.GetScriptPathMarkup(trimmed, options.PathOrName);
            };

            selectedScriptFilePath = AnsiConsole.Prompt(selection);
        }

        return await RunAsync(serviceProvider, selectedScriptFilePath, options);
    }

    private static async Task<int> RunAsync(IServiceProvider serviceProvider, string scriptFilePath, Options options)
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
        if (script == null)
        {
            var dotNetInfo = serviceProvider.GetRequiredService<IDotNetInfo>();
            var latestInstalledSdkVersion = dotNetInfo.GetLatestSupportedDotNetSdkVersion()?.GetFrameworkVersion();
            if (latestInstalledSdkVersion == null)
            {
                Presenter.Error("Could not find an installed .NET SDK.");
                return 1;
            }

            script = CreateScriptFromFile(scriptFilePath, latestInstalledSdkVersion.Value);
        }

        await ApplyOptionsAsync(script, options, serviceProvider);


        if (options.Verbose) Presenter.Info("Starting run...");
        using var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();

        var scriptRunnerFactory = scope.ServiceProvider.GetRequiredService<IScriptRunnerFactory>();
        var scriptRunner = scriptRunnerFactory.CreateRunner(script);
        scriptRunner.AddOutput(new ActionOutputWriter<object>((o, _) =>
        {
            // When the script process runs it outputs to STDOUT directly; we do not redirect.
            // But errors might occur before the script is run, ie: compilation errors. In that
            // case the script runner will emit those errors using this output handler.
            if (o is ScriptOutput error)
            {
                Presenter.Error(error.Body?.ToString() ?? "An error occured.");
                return;
            }

            Console.WriteLine(o);
        }));

        await scriptRunner.RunScriptAsync(new RunOptions());
        return 0;
    }

    private static Script CreateScriptFromFile(string scriptPath, DotNetFrameworkVersion latestInstalledSdkVersion)
    {
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
                latestInstalledSdkVersion,
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
}
