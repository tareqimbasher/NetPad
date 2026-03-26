using System.Text.RegularExpressions;
using Spectre.Console;

namespace NetPad.Apps.Cli;

public static class Presenter
{
    private static readonly IAnsiConsole _errConsole = AnsiConsole.Create(new AnsiConsoleSettings
    {
        Out = new AnsiConsoleOutput(Console.Error)
    });

    public static string GetScriptPathMarkup(string scriptPath, string? highlight = null)
    {
        if (!string.IsNullOrWhiteSpace(highlight))
        {
            // Regex replace to keep original casing, escaping non-highlighted parts for Spectre markup
            var pattern = Regex.Escape(highlight);
            var parts = Regex.Split(scriptPath, $"({pattern})", RegexOptions.IgnoreCase);
            return string.Concat(parts.Select(part =>
                Regex.IsMatch(part, pattern, RegexOptions.IgnoreCase)
                    ? $"[green]{Markup.Escape(part)}[/]"
                    : Markup.Escape(part)));
        }

        var fileName = Path.GetFileName(scriptPath);
        var dirName = Path.GetDirectoryName(scriptPath);
        dirName = string.IsNullOrWhiteSpace(dirName)
            ? string.Empty
            : $"[dim]{Markup.Escape(dirName)}{Path.DirectorySeparatorChar}[/]";

        return $"{dirName}[green]{Markup.Escape(fileName)}[/]";
    }

    public static int PrintList<T>(
        int numberPadding,
        IEnumerable<T> items,
        Func<T, string> getMarkup,
        int? initialOrder = null)
    {
        initialOrder ??= 0;
        int order = initialOrder.Value;

        foreach (var item in items)
        {
            order++;
            var num = order.ToString().PadLeft(numberPadding);
            var markup = $"[violet]{num}.[/] {getMarkup(item)}";
            AnsiConsole.MarkupLine(markup);
        }

        return order - initialOrder.Value;
    }

    public static void Error(string message)
    {
        _errConsole.MarkupLineInterpolated($"[red]err:[/] {message}");
    }

    public static void Warn(string message)
    {
        _errConsole.MarkupLineInterpolated($"[yellow]wrn:[/] {message}");
    }

    public static void Info(string message)
    {
        _errConsole.MarkupLineInterpolated($"[cyan]inf:[/] {message}");
    }

    public static async Task StatusAsync(string initialStatus, Func<Action<string>, Task> action)
    {
        if (Console.IsErrorRedirected)
        {
            await action(_ => { });
            return;
        }

        await _errConsole.Status()
            .StartAsync(initialStatus, async ctx => { await action(status => ctx.Status = status); });
    }
}
