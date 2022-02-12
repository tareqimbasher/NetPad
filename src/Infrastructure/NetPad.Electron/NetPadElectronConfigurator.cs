using ElectronNET.API;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetPad.Electron.UiInterop;
using NetPad.UiInterop;

namespace NetPad.Electron;

public class NetPadElectronConfigurator : IApplicationConfigurator
{
    public void ConfigureWebHost(IWebHostBuilder webHostBuilder, string[] programArgs)
    {
        webHostBuilder.UseElectron(programArgs);
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new ApplicationInfo
        {
            ApplicationType = ApplicationType.Electron
        });

        services.AddTransient<IUiDialogService, ElectronDialogService>();
        services.AddTransient<IUiWindowService, ElectronWindowService>();
        services.AddTransient<IIpcService, ElectronIpcService>();
    }

    public void Configure(IApplicationBuilder app, IHostEnvironment env)
    {
        Task.Run(async () =>
        {
            await app.ApplicationServices.GetRequiredService<IUiWindowService>().OpenMainWindowAsync();
        });
    }
}
