using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetPad.Apps.Security;
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

        services.AddCors(options => options.AddPolicy(
            "AllowWebShell",
            policy => policy.WithOrigins("http://localhost:57930")
                .AllowAnyHeader()
                .AllowAnyMethod()));
    }

    public void ConfigureRequestPipeline(IApplicationBuilder app, IHostEnvironment env)
    {
        app.UseCors("AllowWebShell");
    }

    public void Initialize(IApplicationBuilder app, IHostEnvironment env)
    {
        var hostInfo = app.ApplicationServices.GetRequiredService<HostInfo>();
        var securityToken = app.ApplicationServices.GetRequiredService<SecurityToken>();

        var displayToken =
#if DEBUG
            "dev";
#else
            securityToken.Token;
#endif

        Console.WriteLine();
        Console.WriteLine("==========================================================");
        Console.WriteLine($"  NetPad is running at: {hostInfo.HostUrl}?token={displayToken}");
        Console.WriteLine("==========================================================");
        Console.WriteLine();
    }

    public void ShowErrorDialog(string title, string content)
    {
    }
}
