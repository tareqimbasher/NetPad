using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace NetPad.Plugins;

public class PluginManager : IPluginManager
{
    private readonly PluginInitialization _pluginInitialization;

    public PluginManager(PluginInitialization pluginInitialization)
    {
        _pluginInitialization = pluginInitialization;
    }

    public void RegisterPlugin(Assembly assembly, IServiceCollection services)
    {
        var pluginTypes = assembly.GetExportedTypes().Where(t => t.IsClass && typeof(IPlugin).IsAssignableFrom(t)).ToArray();

        if (pluginTypes.Length == 0)
        {
            throw new Exception($"Assembly {assembly.GetName()} does not have a public class that " +
                                $"implements the {typeof(IPlugin).FullName} interface.");
        }

        if (pluginTypes.Length > 1)
        {
            throw new Exception($"Assembly {assembly.GetName()} has {pluginTypes.Length} public classes that " +
                                $"implement the {typeof(IPlugin).FullName} interface. Expected only 1.");
        }

        var pluginType = pluginTypes.Single();

        var targetCtor = pluginType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(c =>
            {
                var args = c.GetParameters();
                return args.Length == 1 && args.Single().ParameterType == typeof(PluginInitialization);
            });

        if (targetCtor == null)
        {
            throw new Exception($"Could not find a public constructor in type {pluginType.FullName} that has " +
                                $"a single parameter of type {nameof(PluginInitialization)}.");
        }

        if (targetCtor.Invoke(new object[] { _pluginInitialization }) is not IPlugin plugin)
        {
            throw new Exception($"Could not construct an instance of type {pluginType.FullName}.");
        }

        services.AddSingleton(typeof(IPlugin), plugin);

        plugin.ConfigureServices(services);
    }

    public void ConfigurePlugins(IApplicationBuilder app, IHostEnvironment env)
    {
        foreach (var plugin in app.ApplicationServices.GetServices<IPlugin>())
        {
            plugin.Configure(app, env);
        }
    }
}
