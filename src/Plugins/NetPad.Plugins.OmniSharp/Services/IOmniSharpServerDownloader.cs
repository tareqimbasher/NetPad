using System.Runtime.InteropServices;

namespace NetPad.Plugins.OmniSharp.Services;

public interface IOmniSharpServerDownloader
{
    OmniSharpServerLocation? GetDownloadedLocation(OSPlatform platform);
    public Task<OmniSharpServerLocation> DownloadAsync(OSPlatform platform);
}
