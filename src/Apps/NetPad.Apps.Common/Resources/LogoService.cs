namespace NetPad.Apps.Resources;

public class LogoService(HostInfo hostInfo) : ILogoService
{
    public string? GetLogoPath(LogoStyle style, LogoSize size)
    {
        string sizeStr = ((int)size).ToString();
        return Path.Combine(hostInfo.WorkingDirectory, $"wwwroot/logo/{style.ToString().ToLowerInvariant()}/{sizeStr}x{sizeStr}.png");
    }
}
