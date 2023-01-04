using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using NetPad.Application;
using NetPad.Configuration;
using NetPad.Utilities;

namespace NetPad.Plugins.OmniSharp.Services;

internal class DownloadProgress : IProgress<float>
{
    private readonly IAppStatusMessagePublisher _appStatusMessagePublisher;
    private int _lastValueReported;

    public DownloadProgress(IAppStatusMessagePublisher appStatusMessagePublisher)
    {
        _appStatusMessagePublisher = appStatusMessagePublisher;
    }

    public void Report(float value)
    {
        int valueToReport = (int)Math.Ceiling(value * 100);
        if (_lastValueReported == valueToReport)
        {
            return;
        }

        _lastValueReported = valueToReport;
        _appStatusMessagePublisher.PublishAsync($"Downloading OmniSharp... [{valueToReport}%]");
    }
}

public class OmniSharpServerDownloader : IOmniSharpServerDownloader
{
    private readonly HttpClient _httpClient;
    private readonly IAppStatusMessagePublisher _appStatusMessagePublisher;
    private readonly IConfiguration _configuration;

    public OmniSharpServerDownloader(HttpClient httpClient, IAppStatusMessagePublisher appStatusMessagePublisher, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _appStatusMessagePublisher = appStatusMessagePublisher;
        _configuration = configuration;
    }

    public async Task<OmniSharpServerLocation> DownloadAsync(OSPlatform platform)
    {
        try
        {
            if (RuntimeInformation.OSArchitecture != Architecture.X64 && RuntimeInformation.OSArchitecture != Architecture.X86)
            {
                throw new NotSupportedException($"OS Architecture '{RuntimeInformation.OSArchitecture}' is not supported");
            }

            var downloadUrl = GetDownloadUrl(platform);

            var downloadRootDir = GetDownloadRootDirectory();
            if (downloadRootDir.Exists)
            {
                downloadRootDir.Delete(true);
                downloadRootDir.Create();
            }

            var downloadDir = GetDownloadDirectory();

            var start = DateTime.Now;

            _httpClient.Timeout = TimeSpan.FromMinutes(1);
            using var archiveStream = new MemoryStream();
            await _httpClient.DownloadAsync(downloadUrl, archiveStream, new DownloadProgress(_appStatusMessagePublisher));

            await _appStatusMessagePublisher.PublishAsync("Extracting OmniSharp...", persistant: true);
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
                Process.Start("chmod", $"+x {downloadedLocation.ExecutablePath}");
            }

            await _appStatusMessagePublisher.PublishAsync($"OmniSharp download complete (took: {Math.Round((DateTime.Now - start).TotalSeconds, 2)}s)");

            return downloadedLocation;
        }
        catch
        {
            await _appStatusMessagePublisher.PublishAsync("OmniSharp download failed", AppStatusMessagePriority.High, true);
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

        return _configuration.GetValue<string>(settingPath)
               ?? throw new Exception($"No configuration value for OmniSharp version at setting path: '{settingPath}'");
    }

    private string GetDownloadUrl(OSPlatform platform)
    {
        bool is64BitOS = Environment.Is64BitOperatingSystem;
        string settingPath = $"OmniSharp:DownloadUrls:{platform}:{(is64BitOS ? "x64" : "x86")}";

        return _configuration.GetValue<string>(settingPath)
               ?? throw new Exception($"No configuration value for OmniSharp download url at setting path: '{settingPath}'");
    }

    private string GetExecutableFileName(OSPlatform platform)
    {
        string executableName;

        if (platform == OSPlatform.Linux)
        {
            executableName = "OmniSharp";
        }
        else if (platform == OSPlatform.OSX)
        {
            executableName = "OmniSharp";
        }
        else if (platform == OSPlatform.Windows)
        {
            executableName = "OmniSharp.exe";
        }
        else
        {
            throw new NotSupportedException($"Platform '{platform}' is not supported");
        }

        return executableName;
    }
}
