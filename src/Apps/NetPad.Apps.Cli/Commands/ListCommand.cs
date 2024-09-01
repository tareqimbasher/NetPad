using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Configuration;
using Spectre.Console;

namespace NetPad.Apps.Cli.Commands;

public static class ListCommand
{
    public static void AddListCommand(this RootCommand parent, IServiceProvider serviceProvider)
    {
        var listCmd = new Command("list", "List known scripts.")
        {
            Aliases = { "ls" }
        };
        parent.Subcommands.Add(listCmd);
        listCmd.SetAction(_ => ListLibraryScripts(serviceProvider));
    }

    private static void ListLibraryScripts(IServiceProvider serviceProvider)
    {
        var settings = serviceProvider.GetRequiredService<Settings>();
        var libDir = new DirectoryInfo(settings.ScriptsDirectoryPath);

        AnsiConsole.MarkupLineInterpolated($"Scripts in your library ([blue]{libDir.FullName}[/]):");
        int order = 0;
        PrintScriptsRecursive(libDir.FullName, libDir, ref order);
    }

    private static void PrintScriptsRecursive(string rootDirPath, DirectoryInfo currentDir, ref int order)
    {
        var scriptPaths = currentDir
            .EnumerateFiles(ScriptFinder.ScriptSearchPattern)
            .OrderBy(f => f.Name)
            .Select(f => f.FullName[(rootDirPath.Length + 1)..]);

        order += Presenter.PrintList(
            4,
            scriptPaths,
            x => Presenter.GetScriptPathMarkup(x),
            order);

        foreach (var subDir in currentDir.EnumerateDirectories())
        {
            PrintScriptsRecursive(rootDirPath, subDir, ref order);
        }
    }
}
