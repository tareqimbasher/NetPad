namespace NetPad.Presentation.Text;

/// <summary>
/// Prepares output for presentation by formatting it as structured text.
/// </summary>
public static class TextPresenter
{
    private static readonly int _maxDepth;

    static TextPresenter()
    {
        var configFileValues = PresentationSettings.GetConfigFileValues();
        var maxDepth = configFileValues.maxDepth ?? PresentationSettings.MaxDepth;

        // Dumpify lib used here doesn't do well with dumping very deep structures and just hangs.
        _maxDepth = maxDepth > 10 ? 10 : (int)maxDepth;
    }

    public static string Serialize(object? value, string? title = null, bool useConsoleColors = true)
    {
        var colors = useConsoleColors
            ? Dumpify.ColorConfig.DefaultColors
            : Dumpify.ColorConfig.NoColors;

        return Dumpify.DumpExtensions.DumpText(value, label: title, colors: colors, maxDepth: _maxDepth);
    }
}
