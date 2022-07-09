using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NetPad.Plugins;

public interface IPlugin
{
    void ConfigureServices(IServiceCollection services);
    void Configure(IApplicationBuilder app, IHostEnvironment env);
}

public record PluginInitialization
{
    public PluginInitialization(IConfiguration configuration, IHostEnvironment hostEnvironment)
    {
        Configuration = configuration;
        HostEnvironment = hostEnvironment;
    }

    public IConfiguration Configuration { get; init; }
    public IHostEnvironment HostEnvironment { get; init; }
}
