using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetPad.UiInterop;
using NetPad.Web.UiInterop;

namespace NetPad.Web;

public class NetPadWebConfigurator : IApplicationConfigurator
{
    public void ConfigureWebHost(IWebHostBuilder webHostBuilder, string[] programArgs)
    {
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new ApplicationInfo
        {
            ApplicationType = ApplicationType.Electron
        });

        services.AddTransient<IUiWindowService, WebWindowService>();
        services.AddTransient<IUiDialogService, WebDialogService>();
        services.AddTransient<IIpcService, SignalRIpcService>();
    }

    public void Configure(IApplicationBuilder app, IHostEnvironment env)
    {
    }
}
