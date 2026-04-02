using System.Diagnostics;
using NetPad.Configuration;
using JsonSerializer = NetPad.Common.JsonSerializer;

namespace NetPad.Apps.Security;

/// <summary>
/// Manages the connection file that allows external tools (e.g. MCP servers) to discover
/// the running NetPad instance's URL and authentication token.
/// </summary>
public static class ConnectionFileManager
{
    private static readonly string _directoryPath = Path.Combine(AppDataProvider.TempDirectoryPath.Path, "hosts");

    private static string GetConnectionFilePath(int pid)
    {
        return Path.Combine(_directoryPath, $"connection-{pid}.json");
    }

    public static void Write(string url, string token, string? shell)
    {
        Directory.CreateDirectory(_directoryPath);

        var info = new ConnectionInfo
        {
            Url = url,
            Token = token,
            Pid = Environment.ProcessId,
            Shell = shell,
            StartedAt = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(info);
        File.WriteAllText(GetConnectionFilePath(Environment.ProcessId), json);
    }

    public static void Delete()
    {
        try
        {
            File.Delete(GetConnectionFilePath(Environment.ProcessId));
        }
        catch
        {
            // Best-effort cleanup
        }
    }

    public static void CleanupStale()
    {
        if (!Directory.Exists(_directoryPath))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(_directoryPath, "connection-*.json"))
        {
            var fileName = Path.GetFileNameWithoutExtension(file);
            var pidStr = fileName.AsSpan("connection-".Length);

            if (!int.TryParse(pidStr, out int pid))
            {
                continue;
            }

            if (pid == Environment.ProcessId)
            {
                continue;
            }

            try
            {
                using var process = Process.GetProcessById(pid);
                // Process is still running
                continue;
            }
            catch (ArgumentException)
            {
                // Process is not running
            }

            try
            {
                File.Delete(file);
            }
            catch
            {
                // Ignore
            }
        }
    }

    private class ConnectionInfo
    {
        public string Url { get; init; } = default!;
        public string Token { get; init; } = default!;
        public int Pid { get; init; }
        public string? Shell { get; init; }
        public DateTime StartedAt { get; init; }
    }
}
