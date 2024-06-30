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
    string? GetLogoPath(LogoStyle style, LogoSize size);
}
