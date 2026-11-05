namespace NetPad.Configuration;

/// <summary>
/// Which ground a theme family paints on. <see cref="System"/> follows the machine's
/// light/dark preference and changes with it while the app is running.
/// </summary>
public enum ThemeMode
{
    System,
    Dark,
    Light
}
