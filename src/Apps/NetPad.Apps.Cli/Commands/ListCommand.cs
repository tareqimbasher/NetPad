using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Configuration;
using Spectre.Console;

namespace NetPad.Apps.Cli.Commands;

public static class ListCommand
{
    public static void AddListCommand(this RootCommand parent, IServiceProvider serviceProvider)
    {
        var listCmd = new Command("list", "List scripts in your library.")
        {
            Aliases = { "ls" }
        };
        parent.Subcommands.Add(listCmd);
        listCmd.SetAction(_ => ListLibraryScripts(serviceProvider));
    }

    private static void ListLibraryScripts(IServiceProvider serviceProvider)
    {
        var settings = serviceProvider.GetRequiredService<Settings>();
        var libDir = settings.ScriptsDirectoryPath;

        AnsiConsole.MarkupLineInterpolated($"Scripts in your library ([violet]{libDir}[/]):");

        var scripts = ScriptFinder.FindMatches(libDir, null);
        var relativePaths = scripts.Select(s => Path.GetRelativePath(libDir, s));

        Presenter.PrintList(
            4,
            relativePaths,
            x => Presenter.GetScriptPathMarkup(x));
    }
}
