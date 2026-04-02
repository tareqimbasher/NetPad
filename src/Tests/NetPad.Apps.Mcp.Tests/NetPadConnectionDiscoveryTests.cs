using System.Diagnostics;
using System.Text.Json;

namespace NetPad.Apps.Mcp.Tests;

public class NetPadConnectionDiscoveryTests : IDisposable
{
    private static readonly string HostsDirectory = Path.Combine(Path.GetTempPath(), "NetPad", "hosts");

    private readonly string? _savedUrl;
    private readonly string? _savedToken;
    private readonly List<string> _createdFiles = [];

    public NetPadConnectionDiscoveryTests()
    {
        _savedUrl = Environment.GetEnvironmentVariable("NETPAD_URL");
        _savedToken = Environment.GetEnvironmentVariable("NETPAD_TOKEN");
        ClearEnvVars();
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("NETPAD_URL", _savedUrl);
        Environment.SetEnvironmentVariable("NETPAD_TOKEN", _savedToken);

        foreach (var file in _createdFiles)
        {
            try { File.Delete(file); }
            catch { /* best effort cleanup */ }
        }
    }

    [Fact]
    public void Discover_WithBothEnvVars_ReturnsConnectionFromEnvVars()
    {
        Environment.SetEnvironmentVariable("NETPAD_URL", "http://localhost:5000");
        Environment.SetEnvironmentVariable("NETPAD_TOKEN", "my-token");

        var connection = NetPadConnectionDiscovery.Discover();

        Assert.Equal("http://localhost:5000", connection.Url);
        Assert.Equal("my-token", connection.Token);
    }

    [Fact]
    public void Discover_WithEnvVars_TrimsTrailingSlashFromUrl()
    {
        Environment.SetEnvironmentVariable("NETPAD_URL", "http://localhost:5000/");
        Environment.SetEnvironmentVariable("NETPAD_TOKEN", "my-token");

        var connection = NetPadConnectionDiscovery.Discover();

        Assert.Equal("http://localhost:5000", connection.Url);
    }

    [Theory]
    [InlineData("http://localhost:5000", null)]
    [InlineData("http://localhost:5000", "")]
    [InlineData("http://localhost:5000", "  ")]
    [InlineData(null, "my-token")]
    [InlineData("", "my-token")]
    [InlineData("  ", "my-token")]
    public void Discover_PartialEnvVars_DoesNotUseEnvVars(string? url, string? token)
    {
        Environment.SetEnvironmentVariable("NETPAD_URL", url);
        Environment.SetEnvironmentVariable("NETPAD_TOKEN", token);

        // With partial env vars, it falls through to file discovery.
        // Create a valid connection file so it doesn't just throw due to missing files.
        var currentPid = Environment.ProcessId;
        WriteConnectionFile("test-partial-env", "http://localhost:9999", "file-token", currentPid);

        var connection = NetPadConnectionDiscovery.Discover();

        Assert.Equal("file-token", connection.Token);
        Assert.Equal("http://localhost:9999", connection.Url);
    }

    [Fact]
    public void Discover_FromFiles_ReturnsConnectionForRunningProcess()
    {
        var currentPid = Environment.ProcessId;
        WriteConnectionFile("test-running", "http://localhost:7777", "running-token", currentPid);

        var connection = NetPadConnectionDiscovery.Discover();

        Assert.Equal("http://localhost:7777", connection.Url);
        Assert.Equal("running-token", connection.Token);
    }

    [Fact]
    public void Discover_FromFiles_PicksMostRecentInstance()
    {
        var currentPid = Environment.ProcessId;
        WriteConnectionFile("test-old", "http://localhost:1111", "old-token", currentPid,
            startedAt: DateTime.UtcNow.AddHours(100));
        WriteConnectionFile("test-new", "http://localhost:2222", "new-token", currentPid,
            startedAt: DateTime.UtcNow.AddHours(200));

        var connection = NetPadConnectionDiscovery.Discover();

        Assert.Equal("http://localhost:2222", connection.Url);
        Assert.Equal("new-token", connection.Token);
    }

    [Fact]
    public void Discover_FromFiles_IgnoresDeadProcesses()
    {
        var currentPid = Environment.ProcessId;
        var deadPid = FindDeadPid();

        // Dead process with newer timestamp — should be ignored
        WriteConnectionFile("test-dead", "http://localhost:1111", "dead-token", deadPid,
            startedAt: DateTime.UtcNow.AddHours(300));
        // Live process with older timestamp — should be picked
        WriteConnectionFile("test-live", "http://localhost:2222", "live-token", currentPid,
            startedAt: DateTime.UtcNow.AddHours(100));

        var connection = NetPadConnectionDiscovery.Discover();

        Assert.Equal("http://localhost:2222", connection.Url);
        Assert.Equal("live-token", connection.Token);
    }

    [Fact]
    public void Discover_AllDeadProcesses_ThrowsInvalidOperationException()
    {
        var deadPid = FindDeadPid();
        WriteConnectionFile("test-alldead", "http://localhost:1111", "dead-token", deadPid);

        // Clear env vars to ensure file path is used
        ClearEnvVars();

        var ex = Assert.Throws<InvalidOperationException>(() => NetPadConnectionDiscovery.Discover());
        Assert.Contains("No running NetPad instance found", ex.Message);
    }

    [Fact]
    public void Discover_FromFiles_TrimsTrailingSlashFromUrl()
    {
        var currentPid = Environment.ProcessId;
        WriteConnectionFile("test-slash", "http://localhost:3333/", "slash-token", currentPid);

        var connection = NetPadConnectionDiscovery.Discover();

        Assert.Equal("http://localhost:3333", connection.Url);
    }

    private void WriteConnectionFile(string suffix, string url, string token, int pid, DateTime? startedAt = null)
    {
        Directory.CreateDirectory(HostsDirectory);

        var info = new
        {
            url,
            token,
            pid,
            shell = "web",
            startedAt = startedAt ?? DateTime.UtcNow.AddHours(50)
        };

        var fileName = $"connection-{suffix}-{Guid.NewGuid():N}.json";
        var filePath = Path.Combine(HostsDirectory, fileName);
        File.WriteAllText(filePath, JsonSerializer.Serialize(info));
        _createdFiles.Add(filePath);
    }

    private static void ClearEnvVars()
    {
        Environment.SetEnvironmentVariable("NETPAD_URL", null);
        Environment.SetEnvironmentVariable("NETPAD_TOKEN", null);
    }

    private static int FindDeadPid()
    {
        // Find a PID that doesn't correspond to a running process
        for (var pid = 99990; pid < 100000; pid++)
        {
            try
            {
                using var _ = Process.GetProcessById(pid);
            }
            catch (ArgumentException)
            {
                return pid;
            }
        }

        // Fallback: very high PID unlikely to exist
        return 2_000_000_000;
    }
}
