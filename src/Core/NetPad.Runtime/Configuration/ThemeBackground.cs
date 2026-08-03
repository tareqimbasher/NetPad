namespace NetPad.Configuration;

/// <summary>
/// Where the app's backgrounds come from. <see cref="Palette"/> takes them from the theme family.
/// <see cref="Neutral"/> uses one neutral set of backgrounds and text per mode (dark/light).
/// </summary>
public enum ThemeBackground
{
    Palette,
    Neutral
}
