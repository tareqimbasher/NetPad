using System.CommandLine;
using System.Globalization;
using NetPad.Configuration;
using NetPad.Utilities;
using Spectre.Console;

namespace NetPad.Apps.Cli.Commands;

public static class LogsCommand
{
    public static void AddLogsCommand(this RootCommand parent, IServiceProvider serviceProvider)
    {
        var logsCmd = new Command("logs", "Display NetPad log files.");
        parent.Subcommands.Add(logsCmd);
        logsCmd.SetAction(_ => PrintLogFiles(serviceProvider));

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
        var files = logsDir.GetInfo()
            .EnumerateFiles()
            .OrderByDescending(x => x.Name);

        var table = new Table
        {
            Border = TableBorder.Rounded,
            ShowHeaders = true,
            BorderStyle = new Style(Color.PaleTurquoise4)
        };

        table.AddColumn(new TableColumn(new Markup("[bold]#[/]")));
        table.AddColumn(new TableColumn(new Markup("[bold]Log File ▼[/]")));
        table.AddColumn(new TableColumn(new Markup("[bold]Size[/]")));
        table.AddColumn(new TableColumn(new Markup("[bold]Last Write (Local time)[/]")));

        int order = 0;
        foreach (var file in files)
        {
            table.AddRow(
                new Markup($"[violet]{++order}[/]"),
                new Markup(file.FullName),
                new Markup(FileSystemUtil.GetReadableFileSize(file.Length)),
                new Markup(file.LastWriteTime.ToString(CultureInfo.CurrentCulture))
            );
        }

        AnsiConsole.Write(table);
    }
}
