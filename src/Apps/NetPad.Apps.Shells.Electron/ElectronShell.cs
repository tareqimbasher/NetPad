using System.Runtime.InteropServices;
using ElectronNET.API;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetPad.Apps.Shells.Electron.BackgroundServices;
using NetPad.Apps.Shells.Electron.UiInterop;
using NetPad.Apps.UiInterop;
using WindowManager = NetPad.Apps.Shells.Electron.UiInterop.WindowManager;

namespace NetPad.Apps.Shells.Electron;

public class ElectronShell : IShell
{
    public void ConfigureWebHost(IWebHostBuilder webHostBuilder, string[] programArgs)
    {
        webHostBuilder.UseElectron(programArgs);
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<WindowManager>();
        services.AddTransient<IUiWindowService, ElectronWindowService>();
        services.AddTransient<IUiDialogService, ElectronDialogService>();

        services.AddHostedService<NotificationBackgroundService>();
    }

    public void Initialize(IApplicationBuilder app, IHostEnvironment env)
    {
        Task.Run(async () =>
        {
            ElectronNET.API.Electron.App.WindowAllClosed += () =>
            {
                // On macOS it is common for applications and their menu bar
                // to stay active until the user quits explicitly with Cmd + Q
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    ElectronNET.API.Electron.App.Quit();
                }
            };

            ElectronNET.API.Electron.App.WillQuit += args =>
            {
                var appLifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
                appLifetime.StopApplication();
                return Task.CompletedTask;
            };

            await app.ApplicationServices.GetRequiredService<IUiWindowService>().OpenMainWindowAsync();
        });
    }

    public void ShowErrorDialog(string title, string content)
    {
        ElectronNET.API.Electron.Dialog.ShowErrorBox(title, content);
    }
}
