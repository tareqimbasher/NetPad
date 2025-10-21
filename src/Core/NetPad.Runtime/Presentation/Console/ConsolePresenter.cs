namespace NetPad.Presentation.Console;

/// <summary>
/// Presents output to user. Optimized for outputting to a Console. In contrast to other Presenters in this library
/// this Presenter handles the process of presentation of the output by printing it directly to the Console.
/// </summary>
public static class ConsolePresenter
{
    private static readonly ConsoleDumpOptions _consoleDumpOptions;

    static ConsolePresenter()
    {
        var configFileValues = PresentationSettings.GetConfigFileValues();
        var configuredMaxDepth = configFileValues.maxDepth ?? PresentationSettings.MaxDepth;

        // Deep trees on the console aren't needed
        var maxDepth = configuredMaxDepth > 10 ? 10 : (int)configuredMaxDepth;

        _consoleDumpOptions = new ConsoleDumpOptions
        {
            ReferenceLoopHandling = ReferenceLoopHandling.IgnoreAndSerializeCyclicReference,
            MaxDepth = maxDepth,
        };
    }

    public static void Serialize(object? value, string? title = null, bool useConsoleColors = true)
    {
        var colors = useConsoleColors
            ? Dumpify.ColorConfig.DefaultColors
            : Dumpify.ColorConfig.NoColors;

        Dumpify.DumpExtensions.Dump(value, label: title, colors: colors, maxDepth: _maxDepth);
    }
}
