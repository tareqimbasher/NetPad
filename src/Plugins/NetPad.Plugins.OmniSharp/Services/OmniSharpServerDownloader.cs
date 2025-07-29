using System.IO.Compression;
using System.Net.Http;
using System.Runtime.InteropServices;
using NetPad.Application;
using NetPad.Configuration;
using NetPad.Utilities;

namespace NetPad.Plugins.OmniSharp.Services;

internal class DownloadProgress(IAppStatusMessagePublisher appStatusMessagePublisher) : IProgress<float>
{
    private int _lastValueReported;

    public void Report(float value)
    {
        int valueToReport = (int)Math.Ceiling(value * 100);
        if (_lastValueReported == valueToReport)
        {
            return;
        }

        _lastValueReported = valueToReport;
        appStatusMessagePublisher.PublishAsync($"Downloading OmniSharp... [{valueToReport}%]");
    }
}

public class OmniSharpServerDownloader(HttpClient httpClient, IAppStatusMessagePublisher appStatusMessagePublisher, IConfiguration configuration)
    : IOmniSharpServerDownloader
{
    public async Task<OmniSharpServerLocation> DownloadAsync(OSPlatform platform)
    {
        try
        {
            PlatformUtil.IsOsArchitectureSupported(throwIfNotSupported: true);

            var downloadUrl = GetDownloadUrl(platform);

            var downloadRootDir = GetDownloadRootDirectory();
            if (downloadRootDir.Exists)
            {
                downloadRootDir.Delete(true);
                downloadRootDir.Create();
            }

            var downloadDir = GetDownloadDirectory();

            var start = DateTime.Now;

            using var archiveStream = new MemoryStream();
            await httpClient.DownloadAsync(downloadUrl, archiveStream, new DownloadProgress(appStatusMessagePublisher));

            await appStatusMessagePublisher.PublishAsync("Extracting OmniSharp...");
            var zipArchive = new ZipArchive(archiveStream);
            zipArchive.ExtractToDirectory(downloadDir.FullName);

            var downloadedLocation = GetDownloadedLocation(platform);

            if (downloadedLocation == null)
            {
                downloadDir.Delete(true);
                throw new Exception($"Could not find executable in download dir '{downloadDir.FullName}'");
            }

            if (platform != OSPlatform.Windows)
            {
                ProcessUtil.MakeExecutable(downloadedLocation.ExecutablePath);
            }

            await appStatusMessagePublisher.PublishAsync($"OmniSharp download complete (took: {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s)");

            return downloadedLocation;
        }
        catch
        {
            await appStatusMessagePublisher.PublishAsync("OmniSharp download failed", AppStatusMessagePriority.High, true);
            throw;
        }
    }

    public OmniSharpServerLocation? GetDownloadedLocation(OSPlatform platform)
    {
        var downloadDir = GetDownloadDirectory();
        if (!downloadDir.Exists)
        {
            return null;
        }

        var executableFileName = GetExecutableFileName(platform);

        var executableFile = new FileInfo(Path.Combine(downloadDir.FullName, executableFileName));

        if (!executableFile.Exists)
        {
            return null;
        }

        return new OmniSharpServerLocation(executableFile.FullName);
    }

    private DirectoryInfo GetDownloadRootDirectory() => new(Path.Combine(AppDataProvider.AppDataDirectoryPath.Path, "OmniSharp"));
    private DirectoryInfo GetDownloadDirectory() => new(Path.Combine(GetDownloadRootDirectory().FullName, GetRequiredVersion()));

    private string GetRequiredVersion()
    {
        string settingPath = "OmniSharp:Version";

        return configuration.GetValue<string>(settingPath)
               ?? throw new Exception($"No configuration value for OmniSharp version at setting path: '{settingPath}'");
    }

    private string GetDownloadUrl(OSPlatform platform)
    {
        // OmniSharp does not provide a FreeBSD-specific build
        if (platform == OSPlatform.FreeBSD)
            platform = OSPlatform.Linux;

        string arch = RuntimeInformation.OSArchitecture.ToString().ToLowerInvariant();
        string settingPath = $"OmniSharp:DownloadUrls:{platform}:{arch}";

        return configuration.GetValue<string>(settingPath)
               ?? throw new Exception($"No configuration value for OmniSharp download url at setting path: '{settingPath}'");
    }

    private string GetExecutableFileName(OSPlatform platform)
    {
        return platform == OSPlatform.Windows ? "OmniSharp.exe" : "OmniSharp";
    }
}
