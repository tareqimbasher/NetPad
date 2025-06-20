using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetPad.Apps.Shells.Tauri.UiInterop;
using NetPad.Apps.UiInterop;

namespace NetPad.Apps.Shells.Tauri;

/// <summary>
/// Configures the application to run as a desktop Tauri (native, rust-based) application.
/// </summary>
public class TauriShell : IShell
{
    public void ConfigureWebHost(IWebHostBuilder webHostBuilder, string[] programArgs)
    {
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<IUiWindowService, TauriWindowService>();
        services.AddTransient<IUiDialogService, TauriDialogService>();

        // Tauri shell has a loader (index.html) that checks when .NET backend has started
        // This loader page needs to be able to make an HTTP request to the backend to confirm it has started.
        services.AddCors(options => options.AddPolicy(
            "AllowTauriShell",
            policy => policy.WithOrigins(
                "tauri://localhost",        // Linux/macOS
                "http://tauri.localhost"    // Windows
            )));
    }

    public void ConfigureRequestPipeline(IApplicationBuilder app, IHostEnvironment env)
    {
        app.UseCors("AllowTauriShell");
    }

    public void Initialize(IApplicationBuilder app, IHostEnvironment env)
    {
    }

    public void ShowErrorDialog(string title, string content)
    {
    }
}
