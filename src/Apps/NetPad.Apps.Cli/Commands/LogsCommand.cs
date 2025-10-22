using System.CommandLine;
using NetPad.Configuration;
using NetPad.Utilities;

namespace NetPad.Apps.Cli.Commands;

public static class LogsCommand
{
    public static void AddLogsCommand(this RootCommand parent, IServiceProvider serviceProvider)
    {
        var logsCmd = new Command("logs", "NetPad log files.");
        parent.Subcommands.Add(logsCmd);

        var listCmd = new Command("list", "List log files.")
        {
            Aliases = { "ls" }
        };
        logsCmd.Subcommands.Add(listCmd);
        listCmd.SetAction(_ => PrintLogFiles(serviceProvider));
    }

    private static void PrintLogFiles(IServiceProvider serviceProvider)
    {
        var logsDir = AppDataProvider.LogDirectoryPath;
        var files = logsDir.GetInfo().EnumerateFiles().OrderBy(x => x.Name);
        Presenter.PrintList(3, files, f =>
            $"{f.FullName} [dim]{FileSystemUtil.GetReadableFileSize(f.Length),15}[/]");
    }
}
