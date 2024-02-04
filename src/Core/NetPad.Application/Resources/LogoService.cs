namespace NetPad.Resources;

public class LogoService : ILogoService
{
    private readonly HostInfo _hostInfo;

    public LogoService(HostInfo hostInfo)
    {
        _hostInfo = hostInfo;
    }

    public string? GetLogoPath(LogoStyle style, LogoSize size)
    {
        string sizeStr = ((int)size).ToString();
        return Path.Combine(_hostInfo.WorkingDirectory, $"wwwroot/logo/{style.ToString().ToLowerInvariant()}/{sizeStr}x{sizeStr}.png");
    }
}
