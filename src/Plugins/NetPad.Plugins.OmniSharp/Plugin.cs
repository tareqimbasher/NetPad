using MediatR;
using NetPad.Apps.Plugins;
using NetPad.Plugins.OmniSharp.BackgroundServices;
using OmniSharp;

namespace NetPad.Plugins.OmniSharp;

public class Plugin : IPlugin
{
    public Plugin(PluginInitialization initialization)
    {
    }

    public string Id => "netpad.plugins.omnisharp";
    public string Name => "OmniSharp";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<OmniSharpServerCatalog>();
        services.AddTransient<IOmniSharpServerLocator, OmniSharpServerLocator>();
        services.AddTransient<IOmniSharpServerDownloader, OmniSharpServerDownloader>();
        services.AddOmniSharpServer();

        services.AddScoped<AppOmniSharpServerAccessor>();
        services.AddScoped<AppOmniSharpServer>(sp => sp.GetRequiredService<AppOmniSharpServerAccessor>().AppOmniSharpServer);

        services.AddHostedService<ServerManagementBackgroundService>();
        services.AddHostedService<DiagnosticsEventsBackgroundService>();
        services.AddHostedService<EventForwardToIpcBackgroundService>();

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(OmniSharpMediatorPipeline<,>));
    }

    public void Configure(IApplicationBuilder app, IHostEnvironment env)
    {
    }

    public Task CleaupAsync()
    {
        Consts.OmniSharpServerProcessesDirectoryPath.DeleteIfExists();
        return Task.CompletedTask;
    }
}
