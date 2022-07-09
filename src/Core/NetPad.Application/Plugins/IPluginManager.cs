using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NetPad.Plugins;

public interface IPluginManager
{
    void RegisterPlugin(Assembly assembly, IServiceCollection services);
    void ConfigurePlugins(IApplicationBuilder app, IHostEnvironment env);
}
