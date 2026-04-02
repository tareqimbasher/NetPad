using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NetPad.Apps.Mcp;

/// <summary>
/// Monitors the NetPad host process and shuts down the MCP server when it exits.
/// </summary>
public class NetPadLifetimeMonitor(
    NetPadConnection connection,
    IHostApplicationLifetime appLifetime,
    ILogger<NetPadLifetimeMonitor> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (connection.Pid is not { } pid)
        {
            logger.LogDebug("No NetPad PID available; lifetime monitoring is disabled");
            return Task.CompletedTask;
        }

        Process? parentProcess;

        try
        {
            parentProcess = Process.GetProcessById(pid);
            parentProcess.EnableRaisingEvents = true;
        }
        catch (ArgumentException)
        {
            logger.LogInformation("NetPad process {Pid} is not running. Shutting down MCP server", pid);
            appLifetime.StopApplication();
            return Task.CompletedTask;
        }

        parentProcess.Exited += (_, _) =>
        {
            logger.LogInformation("NetPad process {Pid} exited. Shutting down MCP server", pid);
            appLifetime.StopApplication();
        };

        logger.LogDebug("Monitoring NetPad process {Pid}", pid);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
