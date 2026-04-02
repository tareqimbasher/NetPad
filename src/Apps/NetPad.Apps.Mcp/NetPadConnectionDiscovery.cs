using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetPad.Apps.Mcp;

/// <summary>
/// Discovers a running NetPad instance by reading connection files or environment variables.
/// </summary>
public static class NetPadConnectionDiscovery
{
    private static readonly string _hostsDirectory = Path.Combine(Path.GetTempPath(), "NetPad", "hosts");

    /// <summary>
    /// Discovers a running NetPad instance.
    /// Priority:
    /// 1. NETPAD_URL + NETPAD_TOKEN environment variables
    /// 2. Connection files in the temp directory, filtered to running PIDs
    /// </summary>
    public static NetPadConnection Discover()
    {
        var envUrl = Environment.GetEnvironmentVariable("NETPAD_URL");
        var envToken = Environment.GetEnvironmentVariable("NETPAD_TOKEN");

        if (!string.IsNullOrWhiteSpace(envUrl) && !string.IsNullOrWhiteSpace(envToken))
        {
            return new NetPadConnection(envUrl.TrimEnd('/'), envToken);
        }

        return DiscoverFromConnectionFiles();
    }

    private static NetPadConnection DiscoverFromConnectionFiles()
    {
        if (!Directory.Exists(_hostsDirectory))
        {
            throw new InvalidOperationException(
                $"No running NetPad instance found. The hosts directory does not exist: {_hostsDirectory}. " +
                "Please start NetPad first, or set the NETPAD_URL and NETPAD_TOKEN environment variables.");
        }

        var connectionFiles = Directory.GetFiles(_hostsDirectory, "connection-*.json");
        if (connectionFiles.Length == 0)
        {
            throw new InvalidOperationException(
                "No running NetPad instance found. No connection files exist. " +
                "Please start NetPad first, or set the NETPAD_URL and NETPAD_TOKEN environment variables.");
        }

        ConnectionInfo? best = null;

        foreach (var file in connectionFiles)
        {
            ConnectionInfo? info;
            try
            {
                var json = File.ReadAllText(file);
                info = JsonSerializer.Deserialize<ConnectionInfo>(json);
            }
            catch
            {
                continue;
            }

            if (info == null || string.IsNullOrWhiteSpace(info.Url) || string.IsNullOrWhiteSpace(info.Token))
            {
                continue;
            }

            if (!IsProcessRunning(info.Pid))
            {
                continue;
            }

            if (best == null || info.StartedAt > best.StartedAt)
            {
                best = info;
            }
        }

        if (best == null)
        {
            throw new InvalidOperationException(
                "No running NetPad instance found. Connection files exist but none correspond to a running process. " +
                "Please start NetPad first, or set the NETPAD_URL and NETPAD_TOKEN environment variables.");
        }

        return new NetPadConnection(best.Url.TrimEnd('/'), best.Token, best.Pid);
    }

    private static bool IsProcessRunning(int pid)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Mirrors the connection file format written by ConnectionFileManager in NetPad.Apps.Common.
    /// </summary>
    private class ConnectionInfo
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = default!;

        [JsonPropertyName("token")]
        public string Token { get; set; } = default!;

        [JsonPropertyName("pid")]
        public int Pid { get; set; }

        [JsonPropertyName("shell")]
        public string? Shell { get; set; }

        [JsonPropertyName("startedAt")]
        public DateTime StartedAt { get; set; }
    }
}
