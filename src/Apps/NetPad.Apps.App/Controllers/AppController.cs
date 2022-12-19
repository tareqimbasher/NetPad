using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NetPad.Application;
using NetPad.Configuration;
using NetPad.Filters;

namespace NetPad.Controllers;

[ApiController]
[Route("app")]
public class AppController : Controller
{
    [HttpGet("identifier")]
    public AppIdentifier GetIdentifier([FromServices] AppIdentifier appIdentifier)
    {
        return appIdentifier;
    }

    [HttpPatch("open-folder-containing-script")]
    public IActionResult OpenFolderContainingScript([FromQuery] string? scriptPath, [FromServices] Settings settings)
    {
        if (scriptPath == null)
            return BadRequest();

        var dirPath = Path.GetDirectoryName(scriptPath);

        if (dirPath == null || !dirPath.StartsWith(settings.ScriptsDirectoryPath))
            return Unauthorized($"Not allowed.");

        if (!Directory.Exists(dirPath))
            return BadRequest($"Directory does not exist at: {dirPath}");

        Process.Start(new ProcessStartInfo
        {
            FileName = dirPath,
            UseShellExecute = true
        });
        return Ok();
    }

    [HttpPatch("open-scripts-folder")]
    public IActionResult OpenScriptsFolder([FromQuery] string? path, [FromServices] Settings settings)
    {
        string sanitized;

        if (string.IsNullOrWhiteSpace(path))
            sanitized = settings.ScriptsDirectoryPath;
        else
            sanitized = Path.Combine(settings.ScriptsDirectoryPath, path.Trim('.', '/', '\\'));

        if (!Directory.Exists(sanitized))
            return BadRequest($"Directory does not exist at: {path}");

        Process.Start(new ProcessStartInfo
        {
            FileName = sanitized,
            UseShellExecute = true
        });
        return Ok();
    }

    [HttpPatch("open-package-cache-folder")]
    public IActionResult OpenPackageCacheFolder([FromServices] Settings settings)
    {
        var path = settings.PackageCacheDirectoryPath;
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
            throw new Exception($"Package cache folder does not exist at: '{settings.PackageCacheDirectoryPath}'");

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
        return Ok();
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
