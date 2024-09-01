using System.Text.RegularExpressions;
using Spectre.Console;

namespace NetPad.Apps.Cli;

public static class Presenter
{
    public static string GetScriptPathMarkup(string scriptPath, string? highlight = null)
    {
        if (!string.IsNullOrWhiteSpace(highlight))
        {
            // Regex replace to keep original casing
            var pattern = Regex.Escape(highlight);
            return Regex.Replace(
                scriptPath,
                pattern,
                m => $"[green]{m.Value}[/]",
                RegexOptions.IgnoreCase
            );
        }

        var fileName = Path.GetFileName(scriptPath);
        var dirName = Path.GetDirectoryName(scriptPath);
        dirName = string.IsNullOrWhiteSpace(dirName) ? string.Empty : $"{dirName}{Path.DirectorySeparatorChar}";

        return $"{dirName}[green]{fileName}[/]";
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
            var markup = $"[blue]{num}.[/] {getMarkup(item)}";
            AnsiConsole.MarkupLine(markup);
        }

        return order - initialOrder.Value;
    }
}
