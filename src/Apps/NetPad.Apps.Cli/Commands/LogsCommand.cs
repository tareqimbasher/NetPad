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
        var logsCmd = new Command("logs", "Display NetPad log files.")
        {
            Aliases = { "log" }
        };
        parent.Subcommands.Add(logsCmd);
        logsCmd.SetAction(_ => ListLogFiles());

        var listCmd = new Command("list", "List log files.")
        {
            Aliases = { "ls" }
        };
        logsCmd.Subcommands.Add(listCmd);
        listCmd.SetAction(_ => ListLogFiles());

        var showNumberArg = new Argument<int>("number")
        {
            Description = "The log file number (from 'list' output)."
        };

        var showCmd = new Command("show", "Print the contents of a log file.");
        logsCmd.Subcommands.Add(showCmd);
        showCmd.Arguments.Add(showNumberArg);
        showCmd.SetAction(p => ShowLogFile(p.GetValue(showNumberArg)));

        var tailNumberArg = new Argument<int>("number")
        {
            Description = "The log file number (from 'list' output)."
        };

        var tailLinesOption = new Option<int>("--lines", "-n")
        {
            Description = "Number of lines to show from the end of the file before following.",
            DefaultValueFactory = _ => 10
        };

        var tailCmd = new Command("tail", "Follow a log file (like tail -f).");
        logsCmd.Subcommands.Add(tailCmd);
        tailCmd.Arguments.Add(tailNumberArg);
        tailCmd.Options.Add(tailLinesOption);
        tailCmd.SetAction(async (p, ct) =>
        {
            var number = p.GetValue(tailNumberArg);
            var lines = p.GetValue(tailLinesOption);
            await TailLogFile(number, lines, ct);
        });

        var removeNumberArg = new Argument<int>("number")
        {
            Description = "The log file number (from 'list' output).",
            Arity = ArgumentArity.ZeroOrOne
        };

        var removeAllOption = new Option<bool>("--all", "-a")
        {
            Description = "Remove all log files.",
            Arity = ArgumentArity.ZeroOrOne
        };

        var removeCmd = new Command("rm", "Remove a log file.");
        logsCmd.Subcommands.Add(removeCmd);
        removeCmd.Arguments.Add(removeNumberArg);
        removeCmd.Options.Add(removeAllOption);
        removeCmd.SetAction(p =>
        {
            var number = p.GetValue(removeNumberArg);
            var all = p.GetValue(removeAllOption);

            if (number > 0 && all)
            {
                Presenter.Error("Cannot specify --all when specifying a log file to remove.");
                return 1;
            }

            if (number == 0 && !all)
            {
                Presenter.Error(
                    "Specify a log file number to remove (use 'list' to see log files), or --all to remove all.");
                return 1;
            }

            return all ? RemoveAllLogFiles() : RemoveLogFileByNumber(number);
        });

        var clearCmd = new Command("clear", "Remove all log files.");
        logsCmd.Subcommands.Add(clearCmd);
        clearCmd.SetAction(_ => RemoveAllLogFiles());
    }

    private static List<FileInfo> GetOrderedLogFiles()
    {
        var dirInfo = AppDataProvider.LogDirectoryPath.GetInfo();
        if (!dirInfo.Exists) return [];

        return dirInfo
            .EnumerateFiles()
            .OrderByDescending(x => x.Name)
            .ToList();
    }

    private static int ListLogFiles()
    {
        var files = GetOrderedLogFiles();

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
        return 0;
    }

    private static FileInfo? GetLogFileByNumber(int number)
    {
        var files = GetOrderedLogFiles();
        if (files.Count == 0)
        {
            AnsiConsole.MarkupLine("no log files found");
            return null;
        }

        if (number < 1 || number > files.Count)
        {
            Presenter.Error($"Invalid log file number. Must be between 1 and {files.Count}.");
            return null;
        }

        return files[number - 1];
    }

    private static int ShowLogFile(int number)
    {
        var file = GetLogFileByNumber(number);
        if (file == null) return 1;

        using var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        Console.Write(reader.ReadToEnd());
        return 0;
    }

    private static async Task<int> TailLogFile(int number, int initialLines, CancellationToken cancellationToken)
    {
        var file = GetLogFileByNumber(number);
        if (file == null) return 1;

        using var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

        // Print the last N lines first
        if (initialLines > 0 && stream.Length > 0)
        {
            Console.Write(ReadTailLines(stream, initialLines));
        }
        else
        {
            stream.Seek(0, SeekOrigin.End);
        }

        // Follow new content
        // Note: ReadLineAsync returns partial content at EOF even without a \n, and WriteLine
        // adds one, so a write split across flushes would get a spurious newline.
        // Serilog writes complete lines so this is fine in practice.
        using var reader = new StreamReader(stream);
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync();
                if (line != null)
                {
                    Console.WriteLine(line);
                }
                else
                {
                    await Task.Delay(250, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Ctrl+C — exit gracefully
        }

        return 0;
    }

    private static string ReadTailLines(FileStream stream, int lineCount)
    {
        const int bufferSize = 4096;
        var buffer = new byte[bufferSize];
        long position = stream.Length;
        int linesFound = 0;
        bool atEnd = true; // Track whether we're at the very end of the file
        var chunks = new List<byte[]>();

        while (position > 0 && linesFound < lineCount)
        {
            int toRead = (int)Math.Min(bufferSize, position);
            position -= toRead;
            stream.Seek(position, SeekOrigin.Begin);
            int bytesRead = stream.Read(buffer, 0, toRead);

            var chunk = new byte[bytesRead];
            Array.Copy(buffer, chunk, bytesRead);
            chunks.Insert(0, chunk);

            for (int i = bytesRead - 1; i >= 0; i--)
            {
                if (buffer[i] == '\n')
                {
                    // A trailing newline at EOF is not a line boundary — skip it
                    if (atEnd && i == bytesRead - 1)
                    {
                        atEnd = false;
                        continue;
                    }

                    linesFound++;
                    if (linesFound >= lineCount)
                    {
                        chunks[0] = chunks[0][(i + 1)..];
                        break;
                    }
                }

                atEnd = false;
            }
        }

        stream.Seek(0, SeekOrigin.End);

        using var ms = new MemoryStream();
        foreach (var chunk in chunks)
        {
            ms.Write(chunk);
        }

        return System.Text.Encoding.UTF8.GetString(ms.ToArray());
    }

    private static int RemoveAllLogFiles()
    {
        var files = GetOrderedLogFiles();
        if (files.Count == 0)
        {
            AnsiConsole.MarkupLine("no log files found");
            return 0;
        }

        foreach (var file in files)
        {
            Try.Run(() => file.Delete());
        }

        AnsiConsole.MarkupLine("[green]success:[/] all log files were removed");
        return 0;
    }

    private static int RemoveLogFileByNumber(int number)
    {
        var file = GetLogFileByNumber(number);
        if (file == null) return 1;

        Try.Run(() => file.Delete());
        AnsiConsole.MarkupLineInterpolated($"[green]success:[/] log file '{file.Name}' was removed");
        return 0;
    }
}
