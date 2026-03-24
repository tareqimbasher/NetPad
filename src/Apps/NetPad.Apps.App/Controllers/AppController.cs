using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NetPad.Application;
using NetPad.Apps.CQs;
using NetPad.Apps.UiInterop;
using NetPad.Configuration;
using NetPad.DotNet;
using NetPad.Host.Filters;
using NetPad.Scripts;

namespace NetPad.Controllers;

[ApiController]
[Route("app")]
public class AppController(ILogger<AppController> logger) : ControllerBase
{
    [HttpGet("identifier")]
    public AppIdentifier GetIdentifier([FromServices] AppIdentifier appIdentifier)
    {
        return appIdentifier;
    }

    [HttpGet("latest-version")]
    public async Task<string?> GetLatestVersion(
        [FromServices] HttpClient httpClient,
        [FromServices] AppIdentifier appIdentifier)
    {
        try
        {
            var includePreRelease = SemanticVersion.TryParse(appIdentifier.ProductVersion, out var currentVersion)
                                    && currentVersion.IsPrerelease;

            if (includePreRelease)
            {
                return await GetLatestVersionIncludingPreReleases(httpClient);
            }

            return await GetLatestStableVersion(httpClient);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting latest version");
        }

        return null;
    }

    private async Task<string?> GetLatestStableVersion(HttpClient httpClient)
    {
        var json = await FetchGitHubJsonAsync(httpClient,
            "https://api.github.com/repos/tareqimbasher/netpad/releases/latest");

        if (json == null)
        {
            return null;
        }

        using var jsonDocument = JsonDocument.Parse(json);
        var latestVersion = jsonDocument.RootElement.GetProperty("tag_name").GetString();
        latestVersion = latestVersion?.TrimStart('v');

        if (latestVersion == null || !SemanticVersion.TryParse(latestVersion, out var version))
        {
            return null;
        }

        return version.ToString();
    }

    private async Task<string?> GetLatestVersionIncludingPreReleases(HttpClient httpClient)
    {
        var json = await FetchGitHubJsonAsync(httpClient,
            "https://api.github.com/repos/tareqimbasher/netpad/releases?per_page=20");

        if (json == null)
        {
            return null;
        }

        SemanticVersion? highest = null;

        using var jsonDocument = JsonDocument.Parse(json);

        foreach (var release in jsonDocument.RootElement.EnumerateArray())
        {
            if (release.TryGetProperty("draft", out var draft) && draft.GetBoolean())
            {
                continue;
            }

            var tagName = release.GetProperty("tag_name").GetString()?.TrimStart('v');

            if (tagName == null || !SemanticVersion.TryParse(tagName, out var version))
            {
                continue;
            }

            if (highest == null || version > highest)
            {
                highest = version;
            }
        }

        return highest?.ToString();
    }

    private static async Task<string?> FetchGitHubJsonAsync(HttpClient httpClient, string url)
    {
        return await Retry.ExecuteAsync(2, TimeSpan.FromSeconds(2), async () =>
        {
            var httpMessage = new HttpRequestMessage(HttpMethod.Get, url);
            httpMessage.Headers.Add("User-Agent", "NetPad");

            var result = await httpClient.SendAsync(httpMessage);
            return await result.Content.ReadAsStringAsync();
        });
    }

    [HttpPost("client/ready")]
    public async Task NotifyClientAppIsReady([FromServices] IUiDialogService uiDialogService,
        [FromServices] IMediator mediator)
    {
        var result = await CheckDependencies(mediator);

        if (result.SupportedDotNetSdkVersionsInstalled.Length > 0)
        {
            return;
        }

        await uiDialogService.AlertUserAboutMissingDependencies(result);
    }

    [HttpPatch("check-dependencies")]
    public async Task<AppDependencyCheckResult> CheckDependencies([FromServices] IMediator mediator) =>
        await mediator.Send(new CheckAppDependenciesQuery());

    [HttpPatch("open-folder-containing-script")]
    public void OpenFolderContainingScript([FromQuery] string scriptPath, [FromServices] Settings settings)
    {
        if (string.IsNullOrWhiteSpace(scriptPath))
        {
            throw new Exception("No script path provided");
        }

        var file = new FileInfo(scriptPath);

        if (
            !file.Exists
            || file.Directory?.Exists != true
            || !file.Directory.FullName.StartsWith(settings.ScriptsDirectoryPath)
            || !file.Name.EndsWithIgnoreCase(Script.STANDARD_EXTENSION)
        )
        {
            throw new Exception("Not allowed");
        }

        ProcessUtil.OpenWithDefaultApp(file.Directory.FullName);
    }

    [HttpPatch("open-scripts-folder")]
    public void OpenScriptsFolder([FromQuery] string? path, [FromServices] Settings settings)
    {
        string sanitized;

        if (string.IsNullOrWhiteSpace(path))
        {
            sanitized = settings.ScriptsDirectoryPath;
        }
        else
        {
            sanitized = Path.GetFullPath(Path.Combine(settings.ScriptsDirectoryPath, path.Trim('.', '/', '\\')));
        }

        if (!sanitized.StartsWith(settings.ScriptsDirectoryPath, StringComparison.OrdinalIgnoreCase) ||
            !Directory.Exists(sanitized))
        {
            throw new Exception($"Invalid or non-existent directory: {path}");
        }

        ProcessUtil.OpenWithDefaultApp(sanitized);
    }

    [HttpPatch("open-package-cache-folder")]
    public void OpenPackageCacheFolder([FromServices] Settings settings)
    {
        var path = settings.PackageCacheDirectoryPath;
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            throw new Exception($"Package cache folder does not exist at: '{settings.PackageCacheDirectoryPath}'");

        ProcessUtil.OpenWithDefaultApp(path);
    }

    [HttpGet("dotnet-path")]
    public DotNetPathReport GetDotNetPathReport([FromServices] Settings settings)
    {
        return new DotNetPathResolver().FindDotNetInstallDir(settings);
    }

    [HttpPost("log/{source}")]
    [SilentResponse]
    public void SendRemoteLog(
        LogSource source,
        [FromBody] RemoteLogMessage[] logs,
        [FromServices] ILoggerFactory loggerFactory)
    {
        var loggers = new Dictionary<string, ILogger>();

        foreach (var log in logs.OrderBy(l => l.Date))
        {
            var message = log.Message ?? string.Empty;

            foreach (var logOptionalParam in log.OptionalParams ??= [])
            {
                message += $" {logOptionalParam}";
            }

            var loggerName = $"RemoteLog.{source}.{log.Logger ?? "unknown-logger"}";

            if (!loggers.TryGetValue(loggerName, out var logger))
            {
                logger = loggerFactory.CreateLogger(loggerName);
                loggers.Add(loggerName, logger);
            }

            logger.Log(log.LogLevel, message);
        }
    }

    public enum LogSource
    {
        WebApp = 10,
        ElectronApp = 11
    }

    public class RemoteLogMessage
    {
        public string? Logger { get; set; }
        public LogLevel LogLevel { get; set; }
        public string? Message { get; set; }
        public string?[]? OptionalParams { get; set; }
        public DateTime Date { get; set; }
    }
}
