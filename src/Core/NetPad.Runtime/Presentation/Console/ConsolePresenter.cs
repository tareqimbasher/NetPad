using Dumpy;
using Dumpy.Console;
using Spectre.Console;

namespace NetPad.Presentation.Console;

/// <summary>
/// Presents output to user. Optimized for outputting to a Console. In contrast to other Presenters in this library
/// this Presenter handles the process of presentation of the output by printing it directly to the Console.
/// </summary>
public static class ConsolePresenter
{
    private static readonly ConsoleDumpOptions _coloredOptions;
    private static readonly ConsoleDumpOptions _coloredMinimalOptions;
    private static readonly ConsoleDumpOptions _plainTextOptions;
    private static readonly ConsoleDumpOptions _plainTextMinimalOptions;

    static ConsolePresenter()
    {
        var configFileValues = PresentationSettings.GetConfigFileValues();
        var configuredMaxDepth = configFileValues.maxDepth ?? PresentationSettings.MaxDepth;

        // Deep object graphs are unwieldy on the console
        var maxDepth = configuredMaxDepth > 10 ? 10 : (int)configuredMaxDepth;

        _coloredOptions = new ConsoleDumpOptions
        {
            ReferenceLoopHandling = ReferenceLoopHandling.IgnoreAndSerializeCyclicReference,
            MaxDepth = maxDepth,
        };

        _coloredMinimalOptions = new ConsoleDumpOptions
        {
            ReferenceLoopHandling = ReferenceLoopHandling.IgnoreAndSerializeCyclicReference,
            MaxDepth = maxDepth,
            Tables = { ShowRowSeparators = false, ShowTitles = false }
        };

        var plainTextStyles = StyleOptions.Plain;
        plainTextStyles.TableBorderType = TableBorder.Rounded; // Keep rounded table borders

        _plainTextOptions = new ConsoleDumpOptions
        {
            ReferenceLoopHandling = ReferenceLoopHandling.IgnoreAndSerializeCyclicReference,
            MaxDepth = maxDepth,
            Styles = plainTextStyles
        };

        _plainTextMinimalOptions = new ConsoleDumpOptions
        {
            ReferenceLoopHandling = ReferenceLoopHandling.IgnoreAndSerializeCyclicReference,
            MaxDepth = maxDepth,
            Styles = plainTextStyles,
            Tables = { ShowRowSeparators = false, ShowTitles = false }
        };
    }

    public static void Serialize(object? value, string? title = null, bool plainText = false, bool minimal = false)
    {
        if (!plainText && !minimal)
        {
            value.DumpConsole(title, _coloredOptions);
        }
        else if (!plainText && minimal)
        {
            value.DumpConsole(title, _coloredMinimalOptions);
        }
        else if (plainText && !minimal)
        {
            value.DumpConsole(title, _plainTextOptions);
        }
        else
        {
            value.DumpConsole(title, _plainTextMinimalOptions);
        }
    }
}
