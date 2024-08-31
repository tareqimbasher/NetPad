using NetPad.Configuration;
using NetPad.Utilities;

namespace NetPad.Plugins.OmniSharp.Services;

public sealed class OmniSharpServerLocator(Settings settings, IOmniSharpServerDownloader omniSharpServerDownloader, ILogger<OmniSharpServerLocator> logger)
    : IOmniSharpServerLocator, IDisposable
{
    private readonly ILogger<OmniSharpServerLocator> _logger = logger;
    private static readonly SemaphoreSlim _downloadLock = new(1);

    public async Task<OmniSharpServerLocation?> GetServerLocationAsync()
    {
        if (settings.OmniSharp.ExecutablePath != null)
        {
            return new OmniSharpServerLocation(settings.OmniSharp.ExecutablePath);
        }

        var platform = PlatformUtil.GetOSPlatform();

        // Lock so that multiple threads don't attempt to download at the same time
        await _downloadLock.WaitAsync();

        try
        {
            var downloadLocation = omniSharpServerDownloader.GetDownloadedLocation(platform);

            if (downloadLocation != null)
            {
                return downloadLocation;
            }

            downloadLocation = await omniSharpServerDownloader.DownloadAsync(platform);

            return downloadLocation;
        }
        finally
        {
            _downloadLock.Release();
        }
    }

    public void Dispose()
    {
        _downloadLock.Dispose();
    }
}
