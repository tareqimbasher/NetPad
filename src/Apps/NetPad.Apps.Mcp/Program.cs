using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Protocol;
using NetPad.Apps.Mcp;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
#if DEBUG
builder.Logging.SetMinimumLevel(LogLevel.Debug);
#else
builder.Logging.SetMinimumLevel(LogLevel.Warning);
#endif
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

NetPadConnection connection;
try
{
    connection = NetPadConnectionDiscovery.Discover();
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

builder.Services.AddSingleton(connection);
builder.Services.AddSingleton(new NetPadApiClient(connection));
builder.Services.AddHostedService<NetPadLifetimeMonitor>();

builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new()
        {
            Name = "netpad-mcp",
            Title = "NetPad",
            Description =
                "MCP server for NetPad. Run and manage C# scripts, query databases, and analyze script output.",
            Version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.1.0",
            WebsiteUrl = "https://github.com/tareqimbasher/NetPad",
            Icons =
            [
                new Icon
                {
                    Source =
                        "https://github.com/tareqimbasher/NetPad/blob/e2e2e90b750be3e4565e763b27a763ca4ff4946b/docs/images/logo/circle/128x128.png",
                    MimeType = "image/png",
                }
            ]
        };
    })
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
return 0;
