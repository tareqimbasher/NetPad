namespace NetPad.Presentation.Console;

public static class ConsolePresenter
{
    private static readonly int _maxDepth;

    static ConsolePresenter()
    {
        var configFileValues = PresentationSettings.GetConfigFileValues();
        var maxDepth = configFileValues.maxDepth ?? PresentationSettings.MaxDepth;

        // Dumpify lib used here doesn't do well with dumping very deep structures and just hangs.
        _maxDepth = maxDepth > 10 ? 10 : (int)maxDepth;
    }

    public static void Serialize(object? value, string? title = null, bool useConsoleColors = true)
    {
        var colors = useConsoleColors
            ? Dumpify.ColorConfig.DefaultColors
            : Dumpify.ColorConfig.NoColors;

        Dumpify.DumpExtensions.Dump(value, label: title, colors: colors, maxDepth: _maxDepth);
    }
}
