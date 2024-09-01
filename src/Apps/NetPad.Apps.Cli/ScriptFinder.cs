using NetPad.Configuration;
using NetPad.Scripts;
using Spectre.Console;

namespace NetPad.Apps.Cli;

public class ScriptFinder(Settings settings)
{
    public void ListLibraryScripts()
    {
        var libDir = new DirectoryInfo(settings.ScriptsDirectoryPath);

        AnsiConsole.MarkupLineInterpolated($"Scripts in your library ([maroon]{libDir.FullName}[/]):");
        int order = 1;
        PrintScripts(libDir.FullName, libDir, ref order);
    }

    private void PrintScripts(string libDirPath, DirectoryInfo dir, ref int order)
    {
        var files = dir
            .EnumerateFiles($"*.{Script.STANDARD_EXTENSION_WO_DOT}")
            .OrderBy(f => f.Name)
            .Select(f => f.FullName[(libDirPath.Length + 1)..]);

        foreach (var file in files)
        {
            var num = (order++).ToString().PadLeft(4);
            AnsiConsole.MarkupLineInterpolated($"[purple]{num}.[/] [green]{file}[/]");
        }

        foreach (var subDir in dir.EnumerateDirectories())
        {
            PrintScripts(libDirPath, subDir, ref order);
        }
    }
}
