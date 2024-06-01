using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NetPad.Apps.Plugins;

public interface IPluginManager
{
    IEnumerable<PluginRegistration> PluginRegistrations { get; }
    PluginRegistration RegisterPlugin(Assembly assembly, IServiceCollection services);
    void ConfigurePlugins(IApplicationBuilder app, IHostEnvironment env);
}
