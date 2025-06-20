namespace NetPad.Apps.Resources;

public enum LogoSize
{
    Favicon = 0,
    _32 = 32,
    _64 = 64,
    _128 = 128,
    _256 = 256
}

public enum LogoStyle
{
    Square,
    Circle
}

public interface ILogoService
{
    /// <summary>
    /// Gets the path of the application logo that matches the specified style and size.
    /// </summary>
    string? GetLogoPath(LogoStyle style, LogoSize size);
}
