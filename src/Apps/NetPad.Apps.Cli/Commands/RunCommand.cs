using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Configuration;
using NetPad.ExecutionModel;
using NetPad.Scripts;
using Spectre.Console;

namespace NetPad.Apps.Cli.Commands;

public static class RunCommand
{
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
        var textOption = new Option<string>("-text")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "Print output as formatted text. Useful for then piping to a file.",
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
        runCmd.Options.Add(textOption);
        runCmd.Options.Add(noColorOption);
        runCmd.Options.Add(htmlOption);
        runCmd.Options.Add(verboseOption);
        runCmd.SetAction(async p => await RunScriptAsync(
            p.GetRequiredValue(pathOrNameArg),
            serviceProvider));
    }

    private static async Task<int> RunScriptAsync(string pathOrName, IServiceProvider serviceProvider)
    {
        var settings = serviceProvider.GetRequiredService<Settings>();
        var matches = ScriptFinder.FindMatches(settings.ScriptsDirectoryPath, pathOrName);

        if (matches.Length == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Did not match any known scripts[/]");
            return 1;
        }

        var selected = matches[0];

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
                return Presenter.GetScriptPathMarkup(trimmed, pathOrName);
            };

            selected = AnsiConsole.Prompt(selection);
        }

        await RunAsync(selected, serviceProvider);
        return 0;
    }

    private static async Task RunAsync(string scriptPath, IServiceProvider serviceProvider)
    {
        var scriptRepository = serviceProvider.GetRequiredService<IScriptRepository>();

        Script script;

        try
        {
            script = await scriptRepository.GetAsync(scriptPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[red]Could not load script. {ex.Message}[/]");
            return;
        }

        using var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope();

        await new ScriptEnvironment(script, scope).RunAsync(new RunOptions());
    }
}
