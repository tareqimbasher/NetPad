using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NetPad.Application;
using NetPad.Common;
using NetPad.Configuration;
using NetPad.CQs;
using NetPad.DotNet;
using NetPad.Filters;
using NetPad.UiInterop;
using NetPad.Utilities;

namespace NetPad.Controllers;

[ApiController]
[Route("app")]
public class AppController : Controller
{
    private readonly ILogger<AppController> _logger;

    public AppController(ILogger<AppController> logger)
    {
        _logger = logger;
    }

    [HttpGet("identifier")]
    public AppIdentifier GetIdentifier([FromServices] AppIdentifier appIdentifier)
    {
        return appIdentifier;
    }

    [HttpGet("latest-version")]
    public async Task<string?> GetLatestVersion([FromServices] HttpClient httpClient)
    {
        try
        {
            const string url = "https://github.com/tareqimbasher/NetPad/releases/latest";

            var html = await Retry.ExecuteAsync(2, TimeSpan.FromSeconds(2), async () =>
            {
                var result = await httpClient.GetAsync(url);
                return await result.Content.ReadAsStringAsync();
            });

            if (html == null)
            {
                return null;
            }

            var parts = html.Split("releases/tag/v").Skip(1);

            var sb = new StringBuilder();
            foreach (var part in parts)
            {
                sb.Clear();
                foreach (var c in part)
                {
                    if (c == '.' || char.IsDigit(c)) sb.Append(c);
                    else break;
                }

                var versionStr = sb.ToString();
                if (string.IsNullOrWhiteSpace(versionStr) || !Version.TryParse(versionStr, out var version)) continue;

                return version.ToString();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting latest version");
        }

        return null;
    }

    [HttpPost("client/ready")]
    public async Task NotifyClientAppIsReady([FromServices] IUiDialogService uiDialogService, [FromServices] IMediator mediator)
    {
        var result = await CheckDependencies(mediator);

        if (result.DotNetSdkVersions.Any(v => new DotNetSdkVersion(v).Major == BadGlobals.DotNetVersion.ToString())) return;

        await uiDialogService.AlertUserAboutMissingDependencies(result);
    }

    [HttpPatch("check-dependencies")]
    public async Task<AppDependencyCheckResult> CheckDependencies([FromServices] IMediator mediator) =>
        await mediator.Send(new CheckAppDependenciesQuery());

    [HttpPatch("open-folder-containing-script")]
    public void OpenFolderContainingScript([FromQuery] string? scriptPath, [FromServices] Settings settings)
    {
        if (scriptPath == null)
            throw new Exception("Script has no path");

        var file = new FileInfo(scriptPath);

        if (!file.Exists || file.Directory?.Exists != true)
            throw new Exception("Not allowed");

        if (!file.Directory.FullName.StartsWith(settings.ScriptsDirectoryPath))
            throw new Exception("Not allowed");

        Process.Start(new ProcessStartInfo
        {
            FileName = file.Directory.FullName,
            UseShellExecute = true
        });
    }

    [HttpPatch("open-scripts-folder")]
    public void OpenScriptsFolder([FromQuery] string? path, [FromServices] Settings settings)
    {
        string sanitized;

        if (string.IsNullOrWhiteSpace(path))
            sanitized = settings.ScriptsDirectoryPath;
        else
            sanitized = Path.Combine(settings.ScriptsDirectoryPath, path.Trim('.', '/', '\\'));

        if (!Directory.Exists(sanitized))
            throw new Exception($"Directory does not exist at: {path}");

        Process.Start(new ProcessStartInfo
        {
            FileName = sanitized,
            UseShellExecute = true
        });
    }

    [HttpPatch("open-package-cache-folder")]
    public void OpenPackageCacheFolder([FromServices] Settings settings)
    {
        var path = settings.PackageCacheDirectoryPath;
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            throw new Exception($"Package cache folder does not exist at: '{settings.PackageCacheDirectoryPath}'");

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
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

            foreach (var logOptionalParam in log.OptionalParams ??= Array.Empty<string>())
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
