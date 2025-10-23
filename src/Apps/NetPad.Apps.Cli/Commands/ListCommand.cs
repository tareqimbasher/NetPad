using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using NetPad.Configuration;
using Spectre.Console;

namespace NetPad.Apps.Cli.Commands;

public static class ListCommand
{
    private static readonly EnumerationOptions _scriptEnumerationOptions = new()
    {
        IgnoreInaccessible = true,
        MatchCasing = MatchCasing.CaseInsensitive
    };

    public static void AddListCommand(this RootCommand parent, IServiceProvider serviceProvider)
    {
        var listCmd = new Command("list", "List scripts in your scripts library.")
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

        AnsiConsole.MarkupLineInterpolated($"Scripts in your library ([violet]{libDir.FullName}[/]):");
        int order = 0;
        PrintScriptsRecursive(libDir.FullName, libDir, ref order);
    }

    private static void PrintScriptsRecursive(string rootDirPath, DirectoryInfo currentDir, ref int order)
    {
        IEnumerable<FileInfo> files = [];
        foreach (var extension in ScriptFinder.AutoFindFileExtensions)
        {
            files = files.Concat(currentDir.EnumerateFiles($"*{extension}", _scriptEnumerationOptions));
        }

        var scriptPaths = files
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
