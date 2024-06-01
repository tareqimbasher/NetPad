using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NetPad.Apps.Shells;

/// <summary>
/// A shell that hosts the web interface.
/// </summary>
public interface IShell
{
    void ConfigureWebHost(IWebHostBuilder webHostBuilder, string[] programArgs);
    void ConfigureServices(IServiceCollection services);
    void Initialize(IApplicationBuilder app, IHostEnvironment env);
    void ShowErrorDialog(string title, string content);
}
