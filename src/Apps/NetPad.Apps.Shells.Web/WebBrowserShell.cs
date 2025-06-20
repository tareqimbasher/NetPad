using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetPad.Apps.Shells.Web.UiInterop;
using NetPad.Apps.UiInterop;

namespace NetPad.Apps.Shells.Web;

/// <summary>
/// Configures the application to run as a web application.
/// </summary>
public class WebBrowserShell : IShell
{
    public void ConfigureWebHost(IWebHostBuilder webHostBuilder, string[] programArgs)
    {
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<IUiWindowService, WebWindowService>();
        services.AddTransient<IUiDialogService, WebDialogService>();
    }

    public void ConfigureRequestPipeline(IApplicationBuilder app, IHostEnvironment env)
    {
    }

    public void Initialize(IApplicationBuilder app, IHostEnvironment env)
    {
    }

    public void ShowErrorDialog(string title, string content)
    {
    }
}
