using NetPad.Configuration;
using NetPad.Utilities;

namespace NetPad.Plugins.OmniSharp.Services;

public class OmniSharpServerLocator : IOmniSharpServerLocator
{
    private readonly Settings _settings;
    private readonly IOmniSharpServerDownloader _omniSharpServerDownloader;
    private readonly ILogger<OmniSharpServerLocator> _logger;
    private static readonly SemaphoreSlim _downloadLock = new SemaphoreSlim(1);

    public OmniSharpServerLocator(Settings settings, IOmniSharpServerDownloader omniSharpServerDownloader, ILogger<OmniSharpServerLocator> logger)
    {
        _settings = settings;
        _omniSharpServerDownloader = omniSharpServerDownloader;
        _logger = logger;
    }

    public async Task<OmniSharpServerLocation?> GetServerLocationAsync()
    {
        if (_settings.Editor.CodeCompletion.Provider is not OmniSharpCodeCompletionProviderOptions omniSharpCodeCompletionProviderOptions)
        {
            throw new InvalidOperationException($"Code completion provider must be of type {nameof(OmniSharpCodeCompletionProviderOptions)}");
        }

        if (omniSharpCodeCompletionProviderOptions.ExecutablePath != null && !string.IsNullOrWhiteSpace(omniSharpCodeCompletionProviderOptions.ExecutablePath))
        {
            return new OmniSharpServerLocation(omniSharpCodeCompletionProviderOptions.ExecutablePath);
        }

        var platform = PlatformUtils.GetOSPlatform();

        // Lock so that multiple threads don't attempt to download at the same time
        await _downloadLock.WaitAsync();

        try
        {
            var downloadLocation = _omniSharpServerDownloader.GetDownloadedLocation(platform);

            if (downloadLocation != null)
            {
                return downloadLocation;
            }

            downloadLocation = await _omniSharpServerDownloader.DownloadAsync(platform);

            return downloadLocation;
        }
        finally
        {
            _downloadLock.Release();
        }
    }
}
