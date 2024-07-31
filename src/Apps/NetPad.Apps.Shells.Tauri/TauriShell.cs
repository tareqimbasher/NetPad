using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetPad.Apps.Shells.Tauri.UiInterop;
using NetPad.Apps.UiInterop;

namespace NetPad.Apps.Shells.Tauri;

public class TauriShell : IShell
{
    public void ConfigureWebHost(IWebHostBuilder webHostBuilder, string[] programArgs)
    {
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<IUiWindowService, TauriWindowService>();
        services.AddTransient<IUiDialogService, TauriDialogService>();
    }

    public void Initialize(IApplicationBuilder app, IHostEnvironment env)
    {
    }

    public void ShowErrorDialog(string title, string content)
    {
        throw new NotImplementedException();
    }
}
