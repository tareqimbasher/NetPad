using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NetPad;

public interface IApplicationConfigurator
{
    void ConfigureWebHost(IWebHostBuilder webHostBuilder, string[] programArgs);
    void ConfigureServices(IServiceCollection services);
    void Configure(IApplicationBuilder app, IHostEnvironment env);
    void ShowErrorDialog(string title, string content);
}
