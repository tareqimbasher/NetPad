namespace NetPad.Configuration;

/// <summary>
/// Where the app's backgrounds come from. <see cref="Neutral"/> uses one neutral set of backgrounds and
/// text per mode (dark/light). <see cref="Palette"/> takes them from the theme family.
/// </summary>
public enum ThemeBackground
{
    Neutral,
    Palette
}
