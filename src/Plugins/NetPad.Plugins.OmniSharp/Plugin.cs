using NetPad.Plugins.OmniSharp.BackgroundServices;
using NetPad.Plugins.OmniSharp.Services;
using OmniSharp;

namespace NetPad.Plugins.OmniSharp;

public class Plugin : IPlugin
{
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnviornment;

    public Plugin(PluginInitialization initialization)
    {
        _configuration = initialization.Configuration;
        _hostEnviornment = initialization.HostEnvironment;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<OmniSharpServerCatalog>();
        services.AddTransient<IOmniSharpServerLocator, OmniSharpServerLocator>();
        services.AddTransient<IOmniSharpServerDownloader, OmniSharpServerDownloader>();
        services.AddOmniSharpServer();

        services.AddHostedService<ServerManagementBackgroundService>();
    }

    public void Configure(IApplicationBuilder app, IHostEnvironment env)
    {
    }
}
