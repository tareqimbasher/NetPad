using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace NetPad.Plugins.OmniSharp.Services;

public interface IOmniSharpServerDownloader
{
    OmniSharpServerLocation? GetDownloadedLocation(OSPlatform platform);
    public Task<OmniSharpServerLocation> DownloadAsync(OSPlatform platform);
}
