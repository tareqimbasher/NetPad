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
        var logsCmd = new Command("logs", "Manage NetPad log files.")
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
            Description = "The log file number (from 'list' output)."
        };

        var removeCmd = new Command("rm", "Remove a log file. Use 'clear' to remove all.");
        logsCmd.Subcommands.Add(removeCmd);
        removeCmd.Arguments.Add(removeNumberArg);
        removeCmd.SetAction(p => RemoveLogFileByNumber(p.GetValue(removeNumberArg)));

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

    private static FileStream? TryOpenLogStream(FileInfo file)
    {
        try
        {
            return new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
        catch (FileNotFoundException)
        {
            Presenter.Error($"Log file '{file.Name}' no longer exists.");
        }
        catch (IOException ex)
        {
            Presenter.Error($"Could not open '{file.Name}': {ex.Message}");
        }

        return null;
    }

    private static int ShowLogFile(int number)
    {
        var file = GetLogFileByNumber(number);
        if (file == null) return 1;

        using var stream = TryOpenLogStream(file);
        if (stream == null)
        {
            return 1;
        }

        using var reader = new StreamReader(stream);
        Console.Write(reader.ReadToEnd());
        return 0;
    }

    private static async Task<int> TailLogFile(int number, int initialLines, CancellationToken cancellationToken)
    {
        var file = GetLogFileByNumber(number);
        if (file == null)
        {
            return 1;
        }

        using var stream = TryOpenLogStream(file);
        if (stream == null)
        {
            return 1;
        }

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

            int bytesRead = 0;
            while (bytesRead < toRead)
            {
                int n = stream.Read(buffer, bytesRead, toRead - bytesRead);
                if (n == 0) break;
                bytesRead += n;
            }

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

        var failures = new List<(string Name, string Error)>();
        foreach (var file in files)
        {
            try
            {
                file.Delete();
            }
            catch (Exception ex)
            {
                failures.Add((file.Name, ex.Message));
            }
        }

        if (failures.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]success:[/] all log files were removed");
            return 0;
        }

        int removed = files.Count - failures.Count;
        AnsiConsole.MarkupLineInterpolated(
            $"[yellow]partial:[/] removed {removed} of {files.Count} log files");
        foreach (var (name, error) in failures)
        {
            Presenter.Error($"could not remove '{name}': {error}");
        }

        return 1;
    }

    private static int RemoveLogFileByNumber(int number)
    {
        var file = GetLogFileByNumber(number);
        if (file == null) return 1;

        try
        {
            file.Delete();
        }
        catch (Exception ex)
        {
            Presenter.Error($"could not remove '{file.Name}': {ex.Message}");
            return 1;
        }

        AnsiConsole.MarkupLineInterpolated($"[green]success:[/] log file '{file.Name}' was removed");
        return 0;
    }
}
